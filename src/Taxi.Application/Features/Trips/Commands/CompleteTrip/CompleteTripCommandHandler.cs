using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Common;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.CompleteTrip;

public class CompleteTripCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IClientConfigProvider clientConfig,
    IStripePaymentService stripe,
    INotificationService notifications,
    TimeProvider timeProvider,
    ILogger<CompleteTripCommandHandler> logger)
    : IRequestHandler<CompleteTripCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(CompleteTripCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var adminUserId))
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Auth.Unauthorized, "Unauthorized user."));
        }

        var trip = await _context.Trips
            .Include(t => t.Stops)
            .FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip == null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Trip.NotFound, "Trip not found."));
        }

        if (!currentUser.IsAdmin)
        {
            return Error.Forbidden(LocalizationKeys.Auth.Unauthorized, "Only admins can operate trips.");
        }

        if (trip.AcceptedByAdminId != adminUserId)
        {
            return await TripOwnershipHelper.NotOwnedByCurrentAdminAsync(_context, trip.AcceptedByAdminId, ct);
        }

        // Settle any open waiting meter first so its accrued fee is computed and
        // visible (via the tracked entity) to the invoice that the TripCompleted
        // domain-event handler issues during SaveChangesAsync.
        var now = timeProvider.GetUtcNow();
        var activeWaiting = await _context.TripWaitingSessions
            .FirstOrDefaultAsync(s => s.TripId == trip.Id && s.StoppedAtUtc == null, ct);
        activeWaiting?.Stop(now);

        var transitionResult = trip.Complete(now);
        if (transitionResult.IsFailure)
        {
            return transitionResult.Error;
        }

        await _context.SaveChangesAsync(ct);

        // The trip is now committed; collecting the waiting-fee surcharge is a
        // best-effort side effect that must never fail the completion itself.
        await TrySettleWaitingFeeAsync(trip, activeWaiting?.EstimatedFee ?? 0m, ct);

        return Result.Success;
    }

    // Charges the accrued waiting fee off-session against the card saved during the
    // upfront fare payment. Records a WaitingFee Payment row and, if the charge does
    // not succeed, notifies the passenger to settle it.
    private async Task TrySettleWaitingFeeAsync(Trip trip, decimal fee, CancellationToken ct)
    {
        if (fee <= 0m)
        {
            return;
        }

        if (!clientConfig.GetClientConfig().StripeEnabled)
        {
            return;
        }

        // Reuse the original upfront fare card payment (it has the saved customer +
        // payment method). Cash / one-off-method trips have no reusable card, so the
        // fee stays on the invoice for out-of-band collection.
        var farePayment = await _context.Payments
            .Where(p => p.TripId == trip.Id
                && p.Kind == PaymentKind.Fare
                && p.Method == PaymentMethod.CreditCard
                && p.Status == PaymentStatus.Completed
                && p.StripePaymentIntentId != null)
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (farePayment?.StripePaymentIntentId is not { } fareIntentId)
        {
            return;
        }

        try
        {
            var idempotencyKey = $"waiting-fee-{trip.Id}";
            var chargeResult = await stripe.ChargeWaitingFeeAsync(
                fareIntentId, fee, farePayment.Currency, trip.Id, idempotencyKey, ct);

            if (chargeResult.IsFailure)
            {
                // Hard failure with no PaymentIntent to record — just prompt settlement.
                await NotifyWaitingFeeDueAsync(trip, ct);
                return;
            }

            var surcharge = chargeResult.Value;

            var paymentResult = Payment.CreateWaitingFeeSurcharge(
                Guid.NewGuid(), trip.Id, fee, farePayment.Currency, surcharge.PaymentIntentId);
            if (paymentResult.IsError)
            {
                return;
            }

            var surchargePayment = paymentResult.Value;
            if (surcharge.Succeeded)
            {
                surchargePayment.MarkAsCompleted(surcharge.ChargeId);
            }
            else
            {
                surchargePayment.MarkAsFailed(
                    surcharge.RequiresAction ? "authentication_required" : surcharge.Status,
                    "Off-session waiting-fee charge was not completed.");
            }

            _context.Payments.Add(surchargePayment);
            await _context.SaveChangesAsync(ct);

            if (!surcharge.Succeeded)
            {
                await NotifyWaitingFeeDueAsync(trip, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to settle waiting fee for trip {TripId}.", trip.Id);
        }
    }

    private async Task NotifyWaitingFeeDueAsync(Trip trip, CancellationToken ct)
    {
        try
        {
            await notifications.SendPushNotificationAsync(
                trip.PassengerId,
                LocalizationKeys.Notification.WaitingFeeDueTitle,
                LocalizationKeys.Notification.WaitingFeeDueBody,
                new Dictionary<string, string>
                {
                    { "tripId", trip.Id.ToString() },
                    { "type", "waiting_fee_due" },
                },
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to notify passenger of outstanding waiting fee for trip {TripId}.", trip.Id);
        }
    }
}
