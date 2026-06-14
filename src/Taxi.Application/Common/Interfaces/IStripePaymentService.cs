using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Interfaces;

public interface IStripePaymentService
{
    Task<Result<StripePaymentIntentResult>> CreatePaymentIntentAsync(
        Guid quoteId,
        decimal amount,
        string currency,
        Guid tripId,
        Guid passengerId,
        string? existingStripeCustomerId,
        string? passengerEmail,
        string? passengerPhone,
        string passengerName,
        string? passengerPreferredLanguage,
        CancellationToken ct = default);

    Task<Result<Success>> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);

    // Returns the exact payment-method type Stripe used to settle a charge
    // (e.g. "ideal", "klarna", "card"), or null if it can't be determined.
    // Used to print the precise method on the invoice. Never throws.
    Task<string?> GetChargePaymentMethodTypeAsync(string chargeId, CancellationToken ct = default);

    Task<Result<StripeRefundResult>> CreateRefundAsync(string paymentIntentId, decimal? amount = null, CancellationToken ct = default);

    // Charges an off-session waiting-fee surcharge against the card saved during the
    // original (on-session) upfront payment. Returns the resulting intent status; a
    // soft decline / authentication_required is reported via the result's Status and
    // RequiresAction rather than as a failed Result.
    Task<Result<StripeSurchargeResult>> ChargeWaitingFeeAsync(
        string originalPaymentIntentId,
        decimal amount,
        string currency,
        Guid tripId,
        string idempotencyKey,
        CancellationToken ct = default);

    // Creates an on-session PaymentIntent (with a Stripe payment sheet client secret)
    // so the passenger can settle an outstanding waiting fee from inside the app.
    Task<Result<StripePaymentIntentResult>> CreateWaitingFeePaymentIntentAsync(
        decimal amount,
        string currency,
        Guid tripId,
        Guid passengerId,
        string? existingStripeCustomerId,
        string? passengerEmail,
        string? passengerPhone,
        string passengerName,
        CancellationToken ct = default);
}

public sealed record StripePaymentIntentResult(
    string PaymentIntentId,
    string ClientSecret,
    string PublishableKey,
    string CustomerId,
    string EphemeralKeySecret);

public sealed record StripeRefundResult(string RefundId, decimal Amount, string Currency);

public sealed record StripeSurchargeResult(
    string PaymentIntentId,
    string Status,
    string? ChargeId,
    bool RequiresAction)
{
    public bool Succeeded => string.Equals(Status, "succeeded", StringComparison.OrdinalIgnoreCase);
}
