using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.CancelTrip;

public class CancelTripCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IClientConfigProvider clientConfig,
    IStripePaymentService stripe,
    IRefundLifecycleService refundLifecycle,
    TimeProvider timeProvider,
    ILogger<CancelTripCommandHandler> logger) : IRequestHandler<CancelTripCommand, Result<TripDto>>
{
    public async Task<Result<TripDto>> Handle(CancelTripCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var passengerId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);

        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        // Admins are identified by JWT role (they have no DomainUsers row).
        var isAdmin = currentUser.IsAdmin;

        if (!isAdmin && trip.PassengerId != passengerId)
        {
            return TripErrors.NotOwnedByPassenger;
        }

        if (trip.Status is TripStatus.InProgress
            or TripStatus.Completed
            or TripStatus.Cancelled
            or TripStatus.Refunded
            or TripStatus.PaymentFailed)
        {
            return TripErrors.CannotCancel;
        }

        var preCancelStatus = trip.Status;

        // Fare must be known before the policy block because the arrived-fee case computes
        // a flat deduction (fare − €6.50) rather than a fixed percentage.
        var quote = await context.PricingQuotes.FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);
        var fare = quote?.FinalFare ?? 0;
        var currency = quote?.CurrencyCode ?? "EUR";

        // Free window is 5 minutes from booking creation for all trip types (immediate and scheduled).
        // See CancellationPolicy.IsWithinFreeWindow.
        var isWithinPassengerWindow = CancellationPolicy.IsWithinFreeWindow(
            trip.CreatedAtUtc,
            timeProvider.GetUtcNow());

        // Cancellation policy:
        //   - Admin override: full refund (nothing charged yet => 0).
        //   - Passenger, never charged (AwaitingPayment): no refund.
        //   - Passenger inside the 5-minute free window: free cancellation (100% refund).
        //   - Passenger after the free window: still cancellable, but only 45% refunded.
        //     Applies to all statuses including Arrived — no flat-fee exception.
        CancellationActor actor;
        CancellationReason reason;
        decimal refundPercent;
        decimal refundAmount;

        if (isAdmin)
        {
            actor = CancellationActor.Admin;
            reason = CancellationReason.AdminOverride;
            refundPercent = preCancelStatus == TripStatus.AwaitingPayment
                ? 0
                : CancellationPolicy.WithinWindowRefundPercent;
            refundAmount = Math.Round(fare * refundPercent / 100m, 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            actor = CancellationActor.Passenger;
            if (preCancelStatus == TripStatus.AwaitingPayment)
            {
                reason = CancellationReason.PassengerWithinFiveMinutes;
                refundPercent = 0;
                refundAmount = 0;
            }
            else if (isWithinPassengerWindow)
            {
                reason = CancellationReason.PassengerWithinFiveMinutes;
                refundPercent = CancellationPolicy.WithinWindowRefundPercent;
                refundAmount = Math.Round(fare * refundPercent / 100m, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                reason = CancellationReason.PassengerAfterFiveMinutes;
                refundPercent = CancellationPolicy.AfterWindowRefundPercent;
                refundAmount = Math.Round(fare * refundPercent / 100m, 2, MidpointRounding.AwayFromZero);
            }
        }

        // Load the linked fare Payment (if any) so we know whether to refund or cancel the PaymentIntent.
        var payment = await context.Payments
            .FirstOrDefaultAsync(p => p.TripId == trip.Id && p.Kind == PaymentKind.Fare, ct);

        var cancelResult = trip.Cancel();
        if (cancelResult.IsError)
        {
            return cancelResult.Errors;
        }

        var cancellationResult = TripCancellation.Create(
            Guid.NewGuid(),
            trip.Id,
            actor,
            reason,
            refundPercent,
            refundAmount,
            currency,
            request.Note);

        if (cancellationResult.IsError)
        {
            return cancellationResult.Errors;
        }

        context.TripCancellations.Add(cancellationResult.Value);

        PaymentRefund? trackedRefund = null;

        if (payment is not null)
        {
            var stripeEnabled = clientConfig.GetClientConfig().StripeEnabled;
            if (stripeEnabled &&
                !string.IsNullOrWhiteSpace(payment.StripePaymentIntentId) &&
                preCancelStatus == TripStatus.AwaitingPayment &&
                payment.Status == PaymentStatus.Pending)
            {
                // PaymentSheet was never completed: cancel the intent so the user is never charged.
                var cancelIntentResult = await stripe.CancelPaymentIntentAsync(payment.StripePaymentIntentId, ct);
                if (cancelIntentResult.IsFailure)
                {
                    // Log but do not block trip cancellation: the trip is already cancelled in our domain.
                    logger.LogWarning(
                        "Failed to cancel Stripe PaymentIntent {PaymentIntentId} for trip {TripId}",
                        payment.StripePaymentIntentId,
                        trip.Id);
                }
            }
            else if (payment.Status == PaymentStatus.Completed && refundAmount > 0)
            {
                var sourceType = actor == CancellationActor.Admin
                    ? PaymentRefundSourceType.AdminCancellation
                    : PaymentRefundSourceType.PassengerCancellation;
                var refundResult = await refundLifecycle.RequestRefundAsync(
                    new RefundRequest(
                        payment.Id,
                        refundAmount,
                        sourceType,
                        refundPercent,
                        refundAmount >= payment.Amount,
                        trip.Id,
                        cancellationResult.Value.Id,
                        RequestedByAdminId: actor == CancellationActor.Admin ? passengerId : null,
                        PassengerId: trip.PassengerId),
                    ct);

                if (refundResult.IsFailure)
                {
                    logger.LogWarning(
                        "Failed to request tracked refund for PaymentIntent {PaymentIntentId} on trip {TripId}: {ErrorCode}",
                        payment.StripePaymentIntentId,
                        trip.Id,
                        refundResult.Error.Code);
                }
                else
                {
                    trackedRefund = refundResult.Value;

                    if (refundResult.Value.Status == PaymentRefundStatus.Failed)
                    {
                        logger.LogWarning(
                            "Tracked cancellation refund {RefundId} failed immediately for PaymentIntent {PaymentIntentId} on trip {TripId}",
                            refundResult.Value.Id,
                            payment.StripePaymentIntentId,
                            trip.Id);
                    }
                }
            }
        }

        await context.SaveChangesAsync(ct);

        double? driverLatitude = null;
        double? driverLongitude = null;

        var vehicleType = await context.VehicleTypes.FirstOrDefaultAsync(v => v.Id == trip.VehicleTypeId, ct);
        var vehicleTypeName = vehicleType?.Name.En ?? "Unknown";

        if (trip.DriverId.HasValue)
        {
            var driver = await context.Drivers.FirstOrDefaultAsync(d => d.Id == trip.DriverId.Value, ct);
            if (driver != null)
            {
                driverLatitude = driver.CurrentLat.HasValue ? (double)driver.CurrentLat.Value : null;
                driverLongitude = driver.CurrentLng.HasValue ? (double)driver.CurrentLng.Value : null;
            }
        }

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
            quote?.FinalFare ?? 0,
            quote?.CurrencyCode ?? "EUR",
            trip.CreatedAtUtc,
            trip.ScheduledAtUtc,
            stopDtos,
            null,
            driverLatitude,
            driverLongitude,
            vehicleTypeName,
            cancellationResult.Value.ToDto(),
            PassengerNote: trip.PassengerNote,
            IsAirport: trip.IsAirport,
            FlightNumber: trip.FlightNumber,
            AcceptedByAdminId: trip.AcceptedByAdminId,
            AcceptedAtUtc: trip.AcceptedAtUtc,
            IsScheduled: trip.ScheduledAtUtc.HasValue,
            DispatchWindowOpensAtUtc: trip.DispatchWindowOpensAtUtc,
            CanMarkEnRoute: trip.CanMarkEnRoute(timeProvider.GetUtcNow()),
            AttentionState: trip.GetAttentionState(timeProvider.GetUtcNow()).ToString(),
            Refund: trackedRefund?.ToRefundDto());
    }
}
