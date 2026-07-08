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

    Task<Result<StripeRefundResult>> CreateRefundAsync(
        string paymentIntentId,
        decimal? amount = null,
        CancellationToken ct = default,
        string? idempotencyKey = null);

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

    // Creates a SetupIntent so the customer can save a reusable payment method (usually a card)
    // for future off-session ride-related charges. Returns the payment-sheet (setup mode) details.
    Task<Result<StripeSetupIntentResult>> CreateSetupIntentAsync(
        Guid userId,
        string? existingStripeCustomerId,
        string? userEmail,
        string? userPhone,
        string userName,
        CancellationToken ct = default);

    // Retrieves card metadata (brand / last four / expiry) for a saved payment method so it can
    // be shown in the app. Card data itself is never stored on our side. Returns a failure if the
    // method is missing or is not a reusable card.
    Task<Result<StripePaymentMethodDetails>> GetPaymentMethodDetailsAsync(
        string paymentMethodId,
        CancellationToken ct = default);

    // Detaches a saved payment method from the Stripe customer (best-effort; used on delete).
    Task<Result<Success>> DetachPaymentMethodAsync(string paymentMethodId, CancellationToken ct = default);

    // Charges a specific saved payment method off-session (customer not present) for a ride-related
    // fee. Soft declines / authentication_required surface via the result's Status/RequiresAction.
    Task<Result<StripeSurchargeResult>> ChargeOffSessionAsync(
        string stripeCustomerId,
        string paymentMethodId,
        decimal amount,
        string currency,
        Guid tripId,
        string kind,
        string idempotencyKey,
        CancellationToken ct = default);

    // Creates an on-session PaymentIntent so the customer can top up their in-app wallet
    // balance (Stripe only collects the money; the wallet is credited by the backend ledger
    // once the payment_intent.succeeded webhook arrives). The intent metadata carries
    // {type: wallet_topup, userId, walletTransactionId} so the webhook can resolve it, and the
    // idempotency key is derived from walletTransactionId so a retried request reuses the intent.
    Task<Result<StripePaymentIntentResult>> CreateTopUpPaymentIntentAsync(
        Guid walletTransactionId,
        decimal amount,
        string currency,
        Guid userId,
        string? existingStripeCustomerId,
        string? userEmail,
        string? userPhone,
        string userName,
        CancellationToken ct = default);
}

public sealed record StripePaymentIntentResult(
    string PaymentIntentId,
    string ClientSecret,
    string PublishableKey,
    string CustomerId,
    string EphemeralKeySecret);

public sealed record StripeSetupIntentResult(
    string SetupIntentId,
    string ClientSecret,
    string PublishableKey,
    string CustomerId,
    string EphemeralKeySecret);

public sealed record StripePaymentMethodDetails(
    string Brand,
    string LastFour,
    int ExpiryMonth,
    int ExpiryYear,
    string? CardholderName);

public sealed record StripeRefundResult(
    string RefundId,
    decimal Amount,
    string Currency,
    string? Status = null,
    string? PaymentIntentId = null,
    string? ChargeId = null);

public sealed record StripeSurchargeResult(
    string PaymentIntentId,
    string Status,
    string? ChargeId,
    bool RequiresAction)
{
    public bool Succeeded => string.Equals(Status, "succeeded", StringComparison.OrdinalIgnoreCase);
}
