using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.DriverCancelTrip;

public sealed class DriverCancelTripCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IClientConfigProvider clientConfig,
    IStripePaymentService stripe,
    ILogger<DriverCancelTripCommandHandler> logger) : IRequestHandler<DriverCancelTripCommand, Result<TripDto>>
{
    public async Task<Result<TripDto>> Handle(DriverCancelTripCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var driverUserId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var driver = await context.Drivers.FirstOrDefaultAsync(d => d.UserId == driverUserId, ct);
        if (driver is null)
        {
            return TripErrors.DriverNotFound;
        }

        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (trip.DriverId != driver.Id)
        {
            return Error.Validation(LocalizationKeys.Trip.DriverMismatch, "This trip is not assigned to you.");
        }

        if (trip.Status != TripStatus.DriverArrived || trip.ArrivedAtUtc is null)
        {
            return TripErrors.DriverCancelTooEarly;
        }

        // Airport trips give the passenger a 30-minute free wait before the driver may
        // decline to continue; regular trips use the standard 10-minute no-show window.
        var graceMinutes = trip.IsAirport
            ? TripWaitingSession.AirportGraceMinutes
            : TripWaitingSession.DefaultGraceMinutes;
        var waitingStart = trip.ScheduledAtUtc is { } scheduledAt && scheduledAt > trip.ArrivedAtUtc.Value
            ? scheduledAt
            : trip.ArrivedAtUtc.Value;

        if (DateTimeOffset.UtcNow < waitingStart.AddMinutes(graceMinutes))
        {
            return TripErrors.DriverCancelTooEarly;
        }

        if (!Enum.TryParse<CancellationReason>(request.Reason, ignoreCase: true, out var reason) ||
            reason is not (CancellationReason.PassengerLate
                or CancellationReason.PassengerNoShow
                or CancellationReason.PassengerUnreachable
                or CancellationReason.AirportWaitDeclined))
        {
            return TripErrors.InvalidCancellationReason;
        }

        var quote = await context.PricingQuotes.FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);
        var fare = quote?.FinalFare ?? 0;
        var currency = quote?.CurrencyCode ?? "EUR";
        var refundAmount = Math.Round(fare * 0.20m, 2, MidpointRounding.AwayFromZero);

        var cancellationResult = TripCancellation.Create(
            Guid.NewGuid(),
            trip.Id,
            CancellationActor.Driver,
            reason,
            20,
            refundAmount,
            currency,
            request.Note);
        if (cancellationResult.IsError)
        {
            return cancellationResult.Errors;
        }

        var cancelResult = trip.Cancel();
        if (cancelResult.IsError)
        {
            return cancelResult.Errors;
        }

        context.TripCancellations.Add(cancellationResult.Value);

        var payment = await context.Payments.FirstOrDefaultAsync(
            p => p.TripId == trip.Id && p.Kind == PaymentKind.Fare, ct);
        if (clientConfig.GetClientConfig().StripeEnabled &&
            payment?.Status == PaymentStatus.Completed &&
            !string.IsNullOrWhiteSpace(payment.StripePaymentIntentId) &&
            refundAmount > 0)
        {
            var refundResult = await stripe.CreateRefundAsync(payment.StripePaymentIntentId, refundAmount, ct);
            if (refundResult.IsFailure)
            {
                logger.LogWarning(
                    "Failed to issue driver cancellation partial refund for PaymentIntent {PaymentIntentId} on trip {TripId}",
                    payment.StripePaymentIntentId,
                    trip.Id);
            }
        }

        await context.SaveChangesAsync(ct);

        return new TripDto(
            trip.Id,
            trip.ReferenceCode,
            trip.PassengerId,
            trip.DriverId,
            trip.VehicleTypeId,
            trip.Status.ToString(),
            fare,
            currency,
            trip.CreatedAtUtc,
            trip.ScheduledAtUtc,
            trip.Stops.OrderBy(s => s.Sequence).Select(s => new TripStopDto(s.Coordinate.Latitude, s.Coordinate.Longitude, s.AddressLabel, s.Sequence, s.IsCompleted, s.CompletedAtUtc)).ToList(),
            Cancellation: cancellationResult.Value.ToDto(),
            PassengerNote: trip.PassengerNote);
    }
}
