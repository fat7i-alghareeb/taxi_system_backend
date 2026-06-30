using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Common;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Payments.Commands.HandleStripeWebhook;

public class HandleStripeWebhookCommandHandler(
    IAppDbContext context,
    IStripeWebhookValidator validator,
    IStripePaymentService stripe,
    INotificationService notificationService,
    ITripNotifier tripNotifier,
    ILogger<HandleStripeWebhookCommandHandler> logger)
    : IRequestHandler<HandleStripeWebhookCommand, Result<Success>>
{
    private const string GenericCustomerFailureMessage =
        LocalizationKeys.Payment.RefundCustomerFailureMessage;

    public async Task<Result<Success>> Handle(HandleStripeWebhookCommand request, CancellationToken ct)
    {
        var parseResult = validator.Parse(request.Json, request.Signature);
        if (parseResult.IsFailure)
        {
            return parseResult.Error;
        }

        var evt = parseResult.Value;

        logger.LogInformation(
            "Stripe webhook received: kind={Kind} eventId={EventId} paymentIntentId={PaymentIntentId}",
            evt.Kind,
            evt.EventId,
            evt.PaymentIntentId);

        switch (evt.Kind)
        {
            case StripeWebhookEventKind.PaymentIntentSucceeded:
                return await HandleSucceededAsync(evt, ct);

            case StripeWebhookEventKind.PaymentIntentFailed:
                return await HandleFailedAsync(evt, reason: evt.FailureMessage ?? evt.FailureCode ?? "payment_failed", ct);

            case StripeWebhookEventKind.PaymentIntentCanceled:
                return await HandleFailedAsync(evt, reason: evt.FailureCode ?? "canceled", ct);

            case StripeWebhookEventKind.ChargeRefunded:
                return await HandleChargeRefundedCompatibilityAsync(evt, ct);

            case StripeWebhookEventKind.RefundCreated:
            case StripeWebhookEventKind.RefundUpdated:
            case StripeWebhookEventKind.RefundFailed:
                return await HandleRefundLifecycleAsync(evt, ct);

            case StripeWebhookEventKind.Unhandled:
            default:
                // 200 OK so Stripe stops retrying events we don't subscribe to.
                return Result.Success;
        }
    }

    private async Task<Result<Success>> HandleSucceededAsync(StripeWebhookEvent evt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(evt.PaymentIntentId))
        {
            return PaymentErrors.StripeIntentNotFound;
        }

        var payment = await context.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == evt.PaymentIntentId, ct);

        if (payment is null)
        {
            logger.LogWarning("Stripe succeeded webhook for unknown PaymentIntent {PaymentIntentId}", evt.PaymentIntentId);
            return PaymentErrors.StripeIntentNotFound;
        }

        // Idempotency: if Payment already Completed, skip without raising another domain event.
        if (payment.Status == PaymentStatus.Completed)
        {
            logger.LogDebug("PaymentIntent {PaymentIntentId} already completed; skipping.", evt.PaymentIntentId);
            return Result.Success;
        }

        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == payment.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        // Resolve the exact method (ideal/klarna/card) from the charge so the
        // invoice can print "Betaald via: iDEAL". Best-effort — never blocks completion.
        var methodType = await stripe.GetChargePaymentMethodTypeAsync(evt.ChargeId ?? string.Empty, ct);

        var completeResult = payment.MarkAsCompleted(evt.ChargeId, methodType);
        if (completeResult.IsFailure)
        {
            return completeResult.Error;
        }

        // Payment is the boundary that makes the trip visible for admin
        // acceptance. Notifications are emitted from the committed outbox.
        if (trip.Status == TripStatus.AwaitingPayment)
        {
            var confirmResult = trip.ConfirmPayment();
            if (confirmResult.IsFailure)
            {
                return confirmResult.Error;
            }
        }

        await context.SaveChangesAsync(ct);
        return Result.Success;
    }

    private async Task<Result<Success>> HandleFailedAsync(StripeWebhookEvent evt, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(evt.PaymentIntentId))
        {
            return PaymentErrors.StripeIntentNotFound;
        }

        var payment = await context.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == evt.PaymentIntentId, ct);

        if (payment is null)
        {
            logger.LogWarning("Stripe failure webhook for unknown PaymentIntent {PaymentIntentId}", evt.PaymentIntentId);
            return PaymentErrors.StripeIntentNotFound;
        }

        if (payment.Status == PaymentStatus.Failed)
        {
            return Result.Success;
        }

        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == payment.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        var failedResult = payment.MarkAsFailed(evt.FailureCode, evt.FailureMessage);
        if (failedResult.IsFailure)
        {
            return failedResult.Error;
        }

        if (trip.Status == TripStatus.AwaitingPayment)
        {
            var failResult = trip.MarkPaymentFailed(reason);
            if (failResult.IsFailure)
            {
                return failResult.Error;
            }

            // Restore the quote so the passenger can retry with the same quoteId.
            // Stripe only fires this webhook when the PaymentIntent is explicitly
            // failed or canceled on Stripe's side (e.g. card decline, 24-hour
            // expiry). The Flutter client reuses the existing PaymentIntent on a
            // simple dismiss, so the quote stays consumed in that path — no
            // backend call is made. Here we handle the case where the PI is
            // truly terminal and the client needs to start a fresh requestTrip.
            var quote = await context.PricingQuotes
                .FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);
            if (quote is not null)
            {
                quote.MarkAsUnused();
                logger.LogInformation(
                    "Restored quoteId={QuoteId} to unused after PaymentIntent failure for tripId={TripId}",
                    quote.Id,
                    trip.Id);
            }
        }

        await context.SaveChangesAsync(ct);
        return Result.Success;
    }

    private async Task<Result<Success>> HandleRefundLifecycleAsync(StripeWebhookEvent evt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(evt.RefundId))
        {
            return PaymentErrors.NotFound;
        }

        var refund = await context.PaymentRefunds
            .FirstOrDefaultAsync(refund => refund.StripeRefundId == evt.RefundId, ct);

        if (refund is null)
        {
            logger.LogWarning("Stripe refund webhook for unknown Refund {RefundId}", evt.RefundId);
            return PaymentErrors.NotFound;
        }

        if (refund.LastStripeEventId == evt.EventId)
        {
            return Result.Success;
        }

        var payment = await context.Payments.FirstOrDefaultAsync(payment => payment.Id == refund.PaymentId, ct);
        if (payment is null)
        {
            return PaymentErrors.NotFound;
        }

        switch (evt.Kind)
        {
            case StripeWebhookEventKind.RefundCreated:
                refund.MarkPending(evt.RefundId, evt.PaymentIntentId, evt.ChargeId, evt.EventId);
                break;

            case StripeWebhookEventKind.RefundUpdated:
                if (IsRefundSucceeded(evt.RefundStatus))
                {
                    refund.MarkSucceeded(evt.EventId);
                }
                else if (IsRefundFailed(evt.RefundStatus))
                {
                    refund.MarkFailed(
                        evt.FailureCode ?? evt.RefundStatus,
                        evt.FailureMessage ?? evt.RefundStatus,
                        GenericCustomerFailureMessage,
                        canRetry: true,
                        lastStripeEventId: evt.EventId);
                    await NotifyAdminsRefundFailedAsync(refund, payment, ct);
                }
                else
                {
                    refund.MarkPending(evt.RefundId, evt.PaymentIntentId, evt.ChargeId, evt.EventId);
                }

                break;

            case StripeWebhookEventKind.RefundFailed:
                refund.MarkFailed(
                    evt.FailureCode ?? "refund_failed",
                    evt.FailureMessage ?? "refund_failed",
                    GenericCustomerFailureMessage,
                    canRetry: true,
                    lastStripeEventId: evt.EventId);
                await NotifyAdminsRefundFailedAsync(refund, payment, ct);
                break;
        }

        var stateResult = await RecalculateRefundedPaymentStateAsync(payment, ct);
        if (stateResult.IsFailure)
        {
            return stateResult.Error;
        }

        await context.SaveChangesAsync(ct);
        await NotifyRefundRealtimeAsync(refund, payment, ct);
        return Result.Success;
    }

    private async Task<Result<Success>> HandleChargeRefundedCompatibilityAsync(StripeWebhookEvent evt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(evt.PaymentIntentId))
        {
            return PaymentErrors.StripeIntentNotFound;
        }

        var payment = await context.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == evt.PaymentIntentId, ct);

        if (payment is null)
        {
            logger.LogWarning("Stripe refund webhook for unknown PaymentIntent {PaymentIntentId}", evt.PaymentIntentId);
            return PaymentErrors.StripeIntentNotFound;
        }

        if (payment.Status == PaymentStatus.Refunded)
        {
            return Result.Success;
        }

        var trackedRefunds = await context.PaymentRefunds
            .Where(refund => refund.PaymentId == payment.Id)
            .ToListAsync(ct);
        var totals = PaymentRefundAccounting.Calculate(payment.Amount, trackedRefunds);
        var stripeReportsFullRefund = evt.RefundedAmount.HasValue && evt.RefundedAmount.Value >= payment.Amount;
        if (!stripeReportsFullRefund && !totals.IsFullyRefunded)
        {
            return Result.Success;
        }

        var stateResult = await MarkPaymentAndTripFullyRefundedAsync(
            payment,
            totals.IsFullyRefunded ? totals.SuccessfulAmount : evt.RefundedAmount ?? payment.Amount,
            ct);
        if (stateResult.IsFailure)
        {
            return stateResult.Error;
        }

        await context.SaveChangesAsync(ct);
        return Result.Success;
    }

    private async Task<Result<Success>> RecalculateRefundedPaymentStateAsync(Payment payment, CancellationToken ct)
    {
        var refunds = await context.PaymentRefunds
            .Where(refund => refund.PaymentId == payment.Id)
            .ToListAsync(ct);
        var totals = PaymentRefundAccounting.Calculate(payment.Amount, refunds);
        if (!totals.IsFullyRefunded || payment.Status == PaymentStatus.Refunded)
        {
            return Result.Success;
        }

        return await MarkPaymentAndTripFullyRefundedAsync(payment, totals.SuccessfulAmount, ct);
    }

    private async Task<Result<Success>> MarkPaymentAndTripFullyRefundedAsync(
        Payment payment,
        decimal amount,
        CancellationToken ct)
    {
        var refundedResult = payment.MarkAsRefunded();
        if (refundedResult.IsFailure)
        {
            return refundedResult.Error;
        }

        if (payment.Kind != PaymentKind.Fare)
        {
            return Result.Success;
        }

        var trip = await context.Trips.FirstOrDefaultAsync(trip => trip.Id == payment.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (trip.Status is not (TripStatus.Cancelled or TripStatus.Completed))
        {
            return Result.Success;
        }

        var tripRefundedResult = trip.MarkRefunded(amount);
        return tripRefundedResult.IsFailure ? tripRefundedResult.Error : Result.Success;
    }

    private async Task NotifyAdminsRefundFailedAsync(PaymentRefund refund, Payment payment, CancellationToken ct)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                ["type"] = "refund_failed",
                ["refundId"] = refund.Id.ToString(),
                ["paymentId"] = payment.Id.ToString(),
                ["tripId"] = payment.TripId.ToString(),
                ["status"] = refund.Status.ToString(),
            };

            object[] bodyArgs =
            [
                refund.Amount.ToString("0.00"),
                refund.Currency.ToUpperInvariant(),
                payment.TripId,
            ];

            await notificationService.SendPushNotificationToAdminsAsync(
                LocalizationKeys.Payment.RefundFailedAdminTitle,
                LocalizationKeys.Payment.RefundFailedAdminBody,
                data,
                ct,
                bodyArgs: bodyArgs);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to notify admins about refund failure webhook {RefundId} for payment {PaymentId}",
                refund.Id,
                payment.Id);
        }
    }

    private async Task NotifyRefundRealtimeAsync(PaymentRefund refund, Payment payment, CancellationToken ct)
    {
        try
        {
            await tripNotifier.NotifyRefundLifecycleChangedAsync(
                refund.Id,
                payment.Id,
                refund.TripId ?? payment.TripId,
                refund.PassengerId,
                refund.Status.ToString(),
                refund.Amount,
                refund.Currency,
                refund.RequiresAdminAction,
                refund.CanRetry,
                refund.SourceType.ToString(),
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to broadcast refund webhook lifecycle change {RefundId} for payment {PaymentId}",
                refund.Id,
                payment.Id);
        }
    }

    private static bool IsRefundSucceeded(string? status)
        => string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase);

    private static bool IsRefundFailed(string? status)
        => string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase);
}
