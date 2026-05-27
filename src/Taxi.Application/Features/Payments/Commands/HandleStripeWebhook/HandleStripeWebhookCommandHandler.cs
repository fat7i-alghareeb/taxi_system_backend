using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Payments.Commands.HandleStripeWebhook;

public class HandleStripeWebhookCommandHandler(
    IAppDbContext context,
    IStripeWebhookValidator validator,
    ILogger<HandleStripeWebhookCommandHandler> logger)
    : IRequestHandler<HandleStripeWebhookCommand, Result<Success>>
{
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
                return await HandleRefundedAsync(evt, ct);

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

        payment.MarkAsCompleted(evt.ChargeId);

        // If the trip is still AwaitingPayment, just confirm payment so the trip
        // moves to PendingDriver. Admins/drivers receive the TripRequested event
        // via SignalR and pick it up themselves — no auto-dispatch.
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

        payment.MarkAsFailed(evt.FailureCode, evt.FailureMessage);

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

    private async Task<Result<Success>> HandleRefundedAsync(StripeWebhookEvent evt, CancellationToken ct)
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

        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == payment.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        payment.MarkAsRefunded();

        // Only Cancelled/Completed trips can transition to Refunded per Trip.MarkRefunded.
        if (trip.Status == TripStatus.Cancelled || trip.Status == TripStatus.Completed)
        {
            var refundResult = trip.MarkRefunded(evt.RefundedAmount ?? payment.Amount);
            if (refundResult.IsFailure)
            {
                return refundResult.Error;
            }
        }

        await context.SaveChangesAsync(ct);
        return Result.Success;
    }
}
