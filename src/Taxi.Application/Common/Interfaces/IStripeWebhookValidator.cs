using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Interfaces;

public interface IStripeWebhookValidator
{
    Result<StripeWebhookEvent> Parse(string json, string signatureHeader);
}

public enum StripeWebhookEventKind
{
    PaymentIntentSucceeded,
    PaymentIntentFailed,
    PaymentIntentCanceled,
    ChargeRefunded,
    RefundCreated,
    RefundUpdated,
    RefundFailed,
    Unhandled,
}

public sealed record StripeWebhookEvent(
    string EventId,
    StripeWebhookEventKind Kind,
    string? PaymentIntentId,
    string? ChargeId,
    decimal? AmountReceived,
    string? Currency,
    string? FailureCode,
    string? FailureMessage,
    decimal? RefundedAmount,
    string? RefundId = null,
    string? RefundStatus = null);
