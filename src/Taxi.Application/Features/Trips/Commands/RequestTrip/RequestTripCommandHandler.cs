using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Common;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Trips.Commands.RequestTrip;

public class RequestTripCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IClientConfigProvider clientConfig,
    IStripePaymentService stripe,
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
            return TripErrors.QuoteAlreadyUsed;
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
            var intentResult = await stripe.CreatePaymentIntentAsync(
                quoteId: quote.Id,
                amount: quote.FinalFare,
                currency: quote.CurrencyCode,
                tripId: trip.Id,
                passengerId: passengerId,
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
                Guid.NewGuid(),
                trip.Id,
                quote.FinalFare,
                quote.CurrencyCode,
                intent.PaymentIntentId,
                intent.ClientSecret);

            if (paymentResult.IsFailure)
            {
                return paymentResult.Error;
            }

            _context.Trips.Add(trip);
            _context.Payments.Add(paymentResult.Value);
            AddTripRouteIfAvailable(trip, quote);
            await _context.SaveChangesAsync(ct);

            stripePaymentDto = new StripePaymentDto(
                intent.PaymentIntentId,
                intent.ClientSecret,
                intent.PublishableKey,
                intent.CustomerId,
                intent.EphemeralKeySecret);
        }

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
