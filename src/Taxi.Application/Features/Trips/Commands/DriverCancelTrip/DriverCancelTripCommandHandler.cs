using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Common;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.DriverCancelTrip;

public sealed class DriverCancelTripCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IRefundLifecycleService refundLifecycle,
    TimeProvider timeProvider,
    ILogger<DriverCancelTripCommandHandler> logger) : IRequestHandler<DriverCancelTripCommand, Result<TripDto>>
{
    public async Task<Result<TripDto>> Handle(DriverCancelTripCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAdmin ||
            string.IsNullOrWhiteSpace(currentUser.Id) ||
            !Guid.TryParse(currentUser.Id, out var adminId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "An authenticated admin is required.");
        }

        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (trip.AcceptedByAdminId != adminId)
        {
            return await TripOwnershipHelper.NotOwnedByCurrentAdminAsync(context, trip.AcceptedByAdminId, ct);
        }

        if (trip.Status != TripStatus.Arrived || trip.ArrivedAtUtc is null)
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

        if (timeProvider.GetUtcNow() < waitingStart.AddMinutes(graceMinutes))
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
        var refundAmount = Math.Round(
            fare * CancellationPolicy.DriverCancelRefundPercent / 100m, 2, MidpointRounding.AwayFromZero);

        var cancellationResult = TripCancellation.Create(
            Guid.NewGuid(),
            trip.Id,
            CancellationActor.Admin,
            reason,
            CancellationPolicy.DriverCancelRefundPercent,
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
        if (payment?.Status == PaymentStatus.Completed &&
            !string.IsNullOrWhiteSpace(payment.StripePaymentIntentId) &&
            refundAmount > 0)
        {
            var sourceType = reason == CancellationReason.AirportWaitDeclined
                ? PaymentRefundSourceType.AirportWaitCancellation
                : PaymentRefundSourceType.DriverCancellation;
            var refundResult = await refundLifecycle.RequestRefundAsync(
                new RefundRequest(
                    payment.Id,
                    refundAmount,
                    sourceType,
                    CancellationPolicy.DriverCancelRefundPercent,
                    refundAmount >= payment.Amount,
                    trip.Id,
                    cancellationResult.Value.Id,
                    RequestedByAdminId: adminId,
                    PassengerId: trip.PassengerId),
                ct);

            if (refundResult.IsFailure)
            {
                logger.LogWarning(
                    "Failed to request tracked driver cancellation refund for PaymentIntent {PaymentIntentId} on trip {TripId}: {ErrorCode}",
                    payment.StripePaymentIntentId,
                    trip.Id,
                    refundResult.Error.Code);
            }
            else if (refundResult.Value.Status == PaymentRefundStatus.Failed)
            {
                logger.LogWarning(
                    "Tracked driver cancellation refund {RefundId} failed immediately for PaymentIntent {PaymentIntentId} on trip {TripId}",
                    refundResult.Value.Id,
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
            PassengerNote: trip.PassengerNote,
            IsAirport: trip.IsAirport,
            FlightNumber: trip.FlightNumber,
            AcceptedByAdminId: trip.AcceptedByAdminId,
            AcceptedAtUtc: trip.AcceptedAtUtc,
            IsScheduled: trip.ScheduledAtUtc.HasValue,
            DispatchWindowOpensAtUtc: trip.DispatchWindowOpensAtUtc,
            CanMarkEnRoute: trip.CanMarkEnRoute(timeProvider.GetUtcNow()),
            AttentionState: trip.GetAttentionState(timeProvider.GetUtcNow()).ToString());
    }
}
