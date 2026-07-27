using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Common;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Application.Features.Wallet.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;
using Taxi.Domain.Users;
using Taxi.Domain.Wallet;

namespace Taxi.Application.Features.Trips.Commands.RequestTrip;

public class RequestTripCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IClientConfigProvider clientConfig,
    IStripePaymentService stripe,
    IWalletService wallet,
    IWalletDebtGuard debtGuard,
    TimeProvider timeProvider) : IRequestHandler<RequestTripCommand, Result<TripDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<TripDto>> Handle(RequestTripCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var passengerId))
        {
            return TripErrors.PassengerNotFound;
        }

        var passenger = await _context.DomainUsers
            .FirstOrDefaultAsync(u => u.Id == passengerId && u.Role == UserRole.Passenger, ct);

        if (passenger is null)
        {
            return TripErrors.PassengerNotFound;
        }

        // The authoritative gate. Placed before the quote is touched so it also covers the
        // resume branch below, which would otherwise let a customer in debt re-open a Stripe
        // sheet on an AwaitingPayment trip and ride anyway.
        if (await debtGuard.CheckAsync(passengerId, ct) is { } debtError)
        {
            return debtError;
        }

        // Only one trip may be underway at a time. Placed before the quote is touched so it
        // also covers the resume branch below. The comparison is on when each trip becomes
        // *active* rather than on pickup times: a scheduled trip goes live at
        // pickup - ScheduledEnRouteLeadTime, so an immediate ride booked half an hour before
        // a reservation's dispatch window opens would otherwise slip through.
        if (await HasConflictingTripAsync(passengerId, request.ScheduledAt, ct))
        {
            return TripErrors.ScheduleConflict;
        }

        var quote = await _context.PricingQuotes
            .FirstOrDefaultAsync(q => q.Id == request.QuoteId && q.PassengerId == passengerId, ct);

        if (quote is null)
        {
            return TripErrors.QuoteNotFound;
        }

        if (quote.IsExpired())
        {
            return TripErrors.QuoteExpired;
        }

        if (quote.Used)
        {
            // The quote was already consumed by a previous trip request. If that
            // trip is still awaiting payment (e.g. the passenger closed the Stripe
            // sheet by mistake), resume it instead of failing: re-issue the
            // PaymentIntent idempotently (Stripe keys on quoteId, so the same
            // intent/clientSecret comes back) so the sheet can be reopened. Only a
            // genuinely paid/accepted quote returns the 409 conflict.
            var existingTrip = await _context.Trips
                .Include(t => t.Stops)
                .FirstOrDefaultAsync(t => t.QuoteId == quote.Id && t.PassengerId == passengerId, ct);

            if (existingTrip is null
                || existingTrip.Status != TripStatus.AwaitingPayment
                || !clientConfig.GetClientConfig().StripeEnabled)
            {
                return TripErrors.QuoteAlreadyUsed;
            }

            // Reuse the amount of the existing card payment so a mixed trip re-issues only the
            // card portion (not the full fare). Stripe keys on quoteId, so same amount => same
            // intent. Card-only trips have a full-fare card payment, so this is unchanged for them.
            var existingCardPayment = await _context.Payments
                .Where(p => p.TripId == existingTrip.Id
                    && p.Kind == PaymentKind.Fare
                    && p.Method == PaymentMethod.CreditCard
                    && p.StripePaymentIntentId != null)
                .OrderByDescending(p => p.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);
            var resumeAmount = existingCardPayment?.Amount ?? quote.FinalFare;

            var resumeIntentResult = await stripe.CreatePaymentIntentAsync(
                quoteId: quote.Id,
                amount: resumeAmount,
                currency: quote.CurrencyCode,
                tripId: existingTrip.Id,
                passengerId: passengerId,
                existingStripeCustomerId: passenger.StripeCustomerId,
                passengerEmail: passenger.Email,
                passengerPhone: passenger.Phone,
                passengerName: passenger.Name,
                passengerPreferredLanguage: passenger.PreferredLanguage,
                ct: ct);

            if (resumeIntentResult.IsFailure)
            {
                return resumeIntentResult.Error;
            }

            var resumeIntent = resumeIntentResult.Value;

            if (string.IsNullOrWhiteSpace(passenger.StripeCustomerId))
            {
                passenger.SetStripeCustomerId(resumeIntent.CustomerId);
                await _context.SaveChangesAsync(ct);
            }

            var resumeStripeDto = new StripePaymentDto(
                resumeIntent.PaymentIntentId,
                resumeIntent.ClientSecret,
                resumeIntent.PublishableKey,
                resumeIntent.CustomerId,
                resumeIntent.EphemeralKeySecret);

            return await BuildTripResultAsync(existingTrip, quote, resumeStripeDto, ct);
        }

        var stopResults = request.Stops.Select((s, index) => TripStop.Create(
            new Domain.Trips.Coordinate(s.Latitude, s.Longitude),
            index,
            s.Label)).ToList();

        if (stopResults.Any(r => r.IsFailure))
        {
            return stopResults.First(r => r.IsFailure).Error;
        }

        var stops = stopResults.Select(r => r.Value).ToList();
        var isAirport = request.Stops.FirstOrDefault()?.IsAirport == true;
        var referenceCode = $"TRP-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        var tripResult = Trip.Request(
            Guid.NewGuid(),
            referenceCode,
            passengerId,
            quote,
            stops,
            request.ScheduledAt,
            request.PassengerNote,
            isAirport,
            request.FlightNumber);

        if (tripResult.IsFailure)
        {
            return tripResult.Error;
        }

        var trip = tripResult.Value;

        // Mark quote consumed up front (per plan: quote.Used set when Trip is created).
        quote.MarkAsUsed();

        var stripeEnabled = clientConfig.GetClientConfig().StripeEnabled;
        StripePaymentDto? stripePaymentDto = null;

        if (!stripeEnabled)
        {
            // Paymentless flow still crosses the same confirmation boundary:
            // the trip becomes eligible for manual admin acceptance.
            var confirmResult = trip.ConfirmPayment();
            if (confirmResult.IsFailure)
            {
                return confirmResult.Error;
            }

            _context.Trips.Add(trip);
            AddTripRouteIfAvailable(trip, quote);
            await _context.SaveChangesAsync(ct);
        }
        else
        {
            // The card path (default / absent selection) is unchanged. Wallet and mixed are new,
            // gated behind an explicit selection so existing clients keep the exact card flow.
            var paymentResult = request.PaymentMethod switch
            {
                TripPaymentMethod.Wallet => await HandleWalletPaymentAsync(trip, quote, passenger, ct),
                TripPaymentMethod.Mixed => await HandleMixedPaymentAsync(trip, quote, passenger, ct),
                _ => await HandleCardPaymentAsync(trip, quote, passenger, ct),
            };

            if (paymentResult.IsFailure)
            {
                return paymentResult.Error;
            }

            stripePaymentDto = paymentResult.Value;
        }

        return await BuildTripResultAsync(trip, quote, stripePaymentDto, ct);
    }

    /// <summary>
    /// Whether the passenger already holds a trip whose active window overlaps the
    /// one being booked. AwaitingPayment is excluded via <see cref="TripStatuses.Active"/>,
    /// so a trip being resumed never conflicts with itself and an abandoned
    /// half-paid booking never blocks the next one.
    /// </summary>
    private async Task<bool> HasConflictingTripAsync(
        Guid passengerId,
        DateTimeOffset? scheduledAt,
        CancellationToken ct)
    {
        var newWindowStart = TripScheduleConflict.ActiveWindowStart(
            scheduledAt,
            timeProvider.GetUtcNow());

        // The window maths cannot be translated to SQL, so narrow the rows in the
        // database and decide in memory. A passenger holds a handful of open trips
        // at most.
        var openTrips = await _context.Trips
            .AsNoTracking()
            .Where(t => t.PassengerId == passengerId && TripStatuses.Active.Contains(t.Status))
            .Select(t => new { t.ScheduledAtUtc, t.CreatedAtUtc })
            .ToListAsync(ct);

        return openTrips.Any(
            t => TripScheduleConflict.ConflictsWith(
                newWindowStart,
                t.ScheduledAtUtc,
                t.CreatedAtUtc));
    }

    // Card path — full fare on the Stripe sheet. Unchanged from the original behavior.
    private async Task<Result<StripePaymentDto?>> HandleCardPaymentAsync(
        Trip trip,
        PricingQuote quote,
        User passenger,
        CancellationToken ct)
    {
        var intentResult = await stripe.CreatePaymentIntentAsync(
            quoteId: quote.Id,
            amount: quote.FinalFare,
            currency: quote.CurrencyCode,
            tripId: trip.Id,
            passengerId: passenger.Id,
            existingStripeCustomerId: passenger.StripeCustomerId,
            passengerEmail: passenger.Email,
            passengerPhone: passenger.Phone,
            passengerName: passenger.Name,
            passengerPreferredLanguage: passenger.PreferredLanguage,
            ct: ct);

        if (intentResult.IsFailure)
        {
            return intentResult.Error;
        }

        var intent = intentResult.Value;

        if (string.IsNullOrWhiteSpace(passenger.StripeCustomerId))
        {
            passenger.SetStripeCustomerId(intent.CustomerId);
        }

        var paymentResult = Payment.CreateForStripe(
            Guid.NewGuid(), trip.Id, quote.FinalFare, quote.CurrencyCode,
            intent.PaymentIntentId, intent.ClientSecret);
        if (paymentResult.IsFailure)
        {
            return paymentResult.Error;
        }

        _context.Trips.Add(trip);
        _context.Payments.Add(paymentResult.Value);
        AddTripRouteIfAvailable(trip, quote);
        await _context.SaveChangesAsync(ct);

        return new StripePaymentDto(
            intent.PaymentIntentId, intent.ClientSecret, intent.PublishableKey,
            intent.CustomerId, intent.EphemeralKeySecret);
    }

    // Wallet-only — the whole fare from the balance, synchronously (no Stripe, no webhook).
    private async Task<Result<StripePaymentDto?>> HandleWalletPaymentAsync(
        Trip trip,
        PricingQuote quote,
        User passenger,
        CancellationToken ct)
    {
        var fare = quote.FinalFare;

        // Pre-check so an under-funded wallet-only request persists nothing (no phantom trip).
        var account = await _context.WalletAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == passenger.Id, ct);
        if (account is null || account.Balance < fare)
        {
            return WalletErrors.InsufficientBalance;
        }

        var holdResult = await wallet.TryHoldForTripAsync(
            passenger.Id, trip.Id, fare, quote.CurrencyCode, $"trip-{trip.Id:N}", ct);
        if (holdResult.IsFailure)
        {
            return holdResult.Error;
        }

        if (holdResult.Value.HeldAmount < fare)
        {
            // Balance dropped between the check and the hold (rare race) — release and fail.
            await wallet.ReleaseTripHoldAsync(trip.Id, ct);
            return WalletErrors.InsufficientBalance;
        }

        _context.Trips.Add(trip);
        AddTripRouteIfAvailable(trip, quote);

        var commitResult = await wallet.CommitTripHoldAsync(trip.Id, ct);
        if (commitResult.IsFailure)
        {
            return commitResult.Error;
        }

        AddWalletFarePayment(
            trip.Id, commitResult.Value > 0m ? commitResult.Value : fare,
            quote.CurrencyCode, holdResult.Value.HoldTransactionId);

        var confirmResult = trip.ConfirmPayment();
        if (confirmResult.IsFailure)
        {
            return confirmResult.Error;
        }

        await _context.SaveChangesAsync(ct);
        return Result<StripePaymentDto?>.SuccessOrNull(null);
    }

    // Mixed — wallet first (held), the remainder on the card. The webhook commits the hold on
    // card success and releases it on failure, so wallet money is never lost or double-deducted.
    private async Task<Result<StripePaymentDto?>> HandleMixedPaymentAsync(
        Trip trip,
        PricingQuote quote,
        User passenger,
        CancellationToken ct)
    {
        var fare = quote.FinalFare;

        var holdResult = await wallet.TryHoldForTripAsync(
            passenger.Id, trip.Id, fare, quote.CurrencyCode, $"trip-{trip.Id:N}", ct);
        if (holdResult.IsFailure)
        {
            return holdResult.Error;
        }

        var held = holdResult.Value.HeldAmount;
        var cardPortion = decimal.Round(fare - held, 2, MidpointRounding.AwayFromZero);

        // Wallet fully covers the fare — settle like wallet-only.
        if (cardPortion <= 0m)
        {
            _context.Trips.Add(trip);
            AddTripRouteIfAvailable(trip, quote);

            var commitResult = await wallet.CommitTripHoldAsync(trip.Id, ct);
            if (commitResult.IsFailure)
            {
                return commitResult.Error;
            }

            AddWalletFarePayment(
                trip.Id, commitResult.Value > 0m ? commitResult.Value : fare,
                quote.CurrencyCode, holdResult.Value.HoldTransactionId);

            var confirmResult = trip.ConfirmPayment();
            if (confirmResult.IsFailure)
            {
                return confirmResult.Error;
            }

            await _context.SaveChangesAsync(ct);
            return Result<StripePaymentDto?>.SuccessOrNull(null);
        }

        var intentResult = await stripe.CreatePaymentIntentAsync(
            quoteId: quote.Id,
            amount: cardPortion,
            currency: quote.CurrencyCode,
            tripId: trip.Id,
            passengerId: passenger.Id,
            existingStripeCustomerId: passenger.StripeCustomerId,
            passengerEmail: passenger.Email,
            passengerPhone: passenger.Phone,
            passengerName: passenger.Name,
            passengerPreferredLanguage: passenger.PreferredLanguage,
            ct: ct);

        if (intentResult.IsFailure)
        {
            await wallet.ReleaseTripHoldAsync(trip.Id, ct);
            return intentResult.Error;
        }

        var intent = intentResult.Value;

        if (string.IsNullOrWhiteSpace(passenger.StripeCustomerId))
        {
            passenger.SetStripeCustomerId(intent.CustomerId);
        }

        var cardPaymentResult = Payment.CreateForStripe(
            Guid.NewGuid(), trip.Id, cardPortion, quote.CurrencyCode,
            intent.PaymentIntentId, intent.ClientSecret);
        if (cardPaymentResult.IsFailure)
        {
            await wallet.ReleaseTripHoldAsync(trip.Id, ct);
            return cardPaymentResult.Error;
        }

        _context.Trips.Add(trip);
        _context.Payments.Add(cardPaymentResult.Value);
        AddTripRouteIfAvailable(trip, quote);
        await _context.SaveChangesAsync(ct);

        return new StripePaymentDto(
            intent.PaymentIntentId, intent.ClientSecret, intent.PublishableKey,
            intent.CustomerId, intent.EphemeralKeySecret);
    }

    private void AddWalletFarePayment(Guid tripId, decimal amount, string currency, Guid? holdTransactionId)
    {
        if (amount <= 0m)
        {
            return;
        }

        var paymentResult = Payment.CreateFareWalletPayment(
            Guid.NewGuid(), tripId, amount, currency, holdTransactionId?.ToString());
        if (paymentResult.IsFailure)
        {
            return;
        }

        var payment = paymentResult.Value;
        payment.MarkAsCompleted();
        _context.Payments.Add(payment);
    }

    private async Task<Result<TripDto>> BuildTripResultAsync(
        Trip trip,
        PricingQuote quote,
        StripePaymentDto? stripePaymentDto,
        CancellationToken ct)
    {
        var vehicleType = await _context.VehicleTypes.FirstOrDefaultAsync(v => v.Id == trip.VehicleTypeId, ct);
        var vehicleTypeName = vehicleType?.Name.En ?? "Unknown";

        var stopDtos = trip.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => new TripStopDto(
                s.Coordinate.Latitude,
                s.Coordinate.Longitude,
                s.AddressLabel,
                s.Sequence,
                s.IsCompleted,
                s.CompletedAtUtc))
            .ToList();

        return new TripDto(
            trip.Id,
            trip.ReferenceCode,
            trip.PassengerId,
            trip.DriverId,
            trip.VehicleTypeId,
            trip.Status.ToString(),
            quote.FinalFare,
            quote.CurrencyCode,
            trip.CreatedAtUtc,
            trip.ScheduledAtUtc,
            stopDtos,
            stripePaymentDto,
            null,
            null,
            vehicleTypeName,
            PassengerNote: trip.PassengerNote,
            EncodedOverviewPolyline: quote.EncodedOverviewPolyline,
            RouteSegments: TripRouteSegmentMapper.FromJson(quote.RouteSegmentsJson),
            IsAirport: trip.IsAirport,
            FlightNumber: trip.FlightNumber,
            IsScheduled: trip.ScheduledAtUtc.HasValue,
            DispatchWindowOpensAtUtc: trip.DispatchWindowOpensAtUtc,
            CanMarkEnRoute: trip.CanMarkEnRoute(timeProvider.GetUtcNow()),
            AttentionState: trip.GetAttentionState(timeProvider.GetUtcNow()).ToString());
    }

    private void AddTripRouteIfAvailable(Trip trip, PricingQuote quote)
    {
        if (string.IsNullOrEmpty(quote.EncodedOverviewPolyline) || string.IsNullOrEmpty(quote.RouteSegmentsJson))
        {
            return;
        }

        var totalDistanceMeters = (int)Math.Round(quote.TotalDistanceKm * 1000m);
        var totalDurationSeconds = (int)Math.Round(quote.TotalDurationMin * 60m);

        var routeResult = TripRoute.Create(
            Guid.NewGuid(),
            trip.Id,
            quote.EncodedOverviewPolyline,
            totalDistanceMeters,
            totalDurationSeconds,
            quote.RouteSegmentsJson);

        if (routeResult.IsSuccess)
        {
            _context.TripRoutes.Add(routeResult.Value);
        }
    }
}
