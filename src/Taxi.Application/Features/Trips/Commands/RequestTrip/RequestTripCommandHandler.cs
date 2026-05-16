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
    IStripePaymentService stripe) : IRequestHandler<RequestTripCommand, Result<TripDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<TripDto>> Handle(RequestTripCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var passengerId))
        {
            return TripErrors.PassengerNotFound;
        }

        var passengerExists = await _context.DomainUsers
            .AnyAsync(u => u.Id == passengerId && u.Role == UserRole.Passenger, ct);

        if (!passengerExists)
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
        var referenceCode = $"TRP-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        var tripResult = Trip.Request(
            Guid.NewGuid(),
            referenceCode,
            passengerId,
            quote,
            stops,
            request.ScheduledAt);

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
            // Legacy paymentless flow: immediately confirm payment and assign a driver
            // so the existing client UX (no PaymentSheet) keeps working.
            var confirmResult = trip.ConfirmPayment();
            if (confirmResult.IsFailure)
            {
                return confirmResult.Error;
            }

            var dispatchResult = await TripDispatchHelper.AssignDefaultDriverAsync(trip, _context, ct);
            if (dispatchResult.IsFailure)
            {
                return dispatchResult.Error;
            }

            _context.Trips.Add(trip);
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
                ct);

            if (intentResult.IsFailure)
            {
                return intentResult.Error;
            }

            var intent = intentResult.Value;

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
            await _context.SaveChangesAsync(ct);

            stripePaymentDto = new StripePaymentDto(
                intent.PaymentIntentId,
                intent.ClientSecret,
                intent.PublishableKey);
        }

        var stopDtos = trip.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => new TripStopDto(s.Coordinate.Latitude, s.Coordinate.Longitude))
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
            stripePaymentDto);
    }
}
