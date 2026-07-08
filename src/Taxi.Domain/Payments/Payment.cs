using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Payments;

public sealed class Payment : AuditableEntity
{
    private Payment() { }

    private Payment(
        Guid id,
        Guid tripId,
        decimal amount,
        string currency,
        PaymentMethod method)
        : base(id)
    {
        TripId = tripId;
        Amount = amount;
        Currency = currency;
        Method = method;
        Status = PaymentStatus.Pending;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid TripId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;
    public PaymentMethod Method { get; private set; }

    /// <summary>Distinguishes the upfront fare from a post-trip waiting-fee surcharge.</summary>
    public PaymentKind Kind { get; private set; }

    public PaymentStatus Status { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? TransactionReference { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public string? StripeClientSecret { get; private set; }
    public string? StripeChargeId { get; private set; }

    /// <summary>
    /// The exact Stripe payment-method type used to settle this payment
    /// (e.g. "ideal", "klarna", "card"). Captured from the charge on success
    /// and printed on the invoice. Null for cash / non-Stripe payments.
    /// </summary>
    public string? StripePaymentMethodType { get; private set; }

    public string? LastErrorCode { get; private set; }
    public string? LastErrorMessage { get; private set; }

    public static Result<Payment> Create(
        Guid id,
        Guid tripId,
        decimal amount,
        string currency,
        PaymentMethod method)
    {
        if (amount <= 0)
        {
            return PaymentErrors.InvalidAmount;
        }

        return new Payment(id, tripId, amount, currency, method);
    }

    public static Result<Payment> CreateForStripe(
        Guid id,
        Guid tripId,
        decimal amount,
        string currency,
        string paymentIntentId,
        string clientSecret)
    {
        if (amount <= 0)
        {
            return PaymentErrors.InvalidAmount;
        }

        var payment = new Payment(id, tripId, amount, currency, PaymentMethod.CreditCard)
        {
            StripePaymentIntentId = paymentIntentId,
            StripeClientSecret = clientSecret,
            TransactionReference = paymentIntentId,
        };

        return payment;
    }

    // Creates the ledger entry for an off-session waiting-fee surcharge that is
    // confirmed server-side (no client secret). The caller marks it
    // completed/failed based on the off-session charge result.
    public static Result<Payment> CreateWaitingFeeSurcharge(
        Guid id,
        Guid tripId,
        decimal amount,
        string currency,
        string paymentIntentId)
    {
        if (amount <= 0)
        {
            return PaymentErrors.InvalidAmount;
        }

        var payment = new Payment(id, tripId, amount, currency, PaymentMethod.CreditCard)
        {
            Kind = PaymentKind.WaitingFee,
            StripePaymentIntentId = paymentIntentId,
            TransactionReference = paymentIntentId,
        };

        return payment;
    }

    // Records the wallet-funded portion of a trip fare (wallet-only or the wallet part of a mixed
    // payment). No Stripe intent — the money moves through the wallet ledger. The caller marks it
    // completed once the wallet debit is committed.
    public static Result<Payment> CreateFareWalletPayment(
        Guid id,
        Guid tripId,
        decimal amount,
        string currency,
        string? walletTransactionReference = null)
    {
        if (amount <= 0)
        {
            return PaymentErrors.InvalidAmount;
        }

        var payment = new Payment(id, tripId, amount, currency, PaymentMethod.Wallet)
        {
            Kind = PaymentKind.Fare,
            TransactionReference = walletTransactionReference,
        };

        return payment;
    }

    // Records the wallet-funded portion of a ride-related fee (e.g. a waiting fee paid from the
    // in-app balance). No Stripe intent — the money moves through the wallet ledger. The caller
    // marks it completed once the wallet debit is committed.
    public static Result<Payment> CreateWaitingFeeWalletPayment(
        Guid id,
        Guid tripId,
        decimal amount,
        string currency,
        string? walletTransactionReference = null)
    {
        if (amount <= 0)
        {
            return PaymentErrors.InvalidAmount;
        }

        var payment = new Payment(id, tripId, amount, currency, PaymentMethod.Wallet)
        {
            Kind = PaymentKind.WaitingFee,
            TransactionReference = walletTransactionReference,
        };

        return payment;
    }

    public Result<Success> MarkAsCompleted(string? chargeId = null, string? stripePaymentMethodType = null)
    {
        if (Status == PaymentStatus.Completed)
        {
            return Result.Success;
        }

        Status = PaymentStatus.Completed;
        ProcessedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(chargeId))
        {
            StripeChargeId = chargeId;
            TransactionReference = chargeId;
        }

        if (!string.IsNullOrWhiteSpace(stripePaymentMethodType))
        {
            StripePaymentMethodType = stripePaymentMethodType;
        }

        return Result.Success;
    }

    public Result<Success> MarkAsFailed(string? errorCode = null, string? errorMessage = null)
    {
        Status = PaymentStatus.Failed;
        ProcessedAtUtc = DateTime.UtcNow;
        LastErrorCode = errorCode;
        LastErrorMessage = errorMessage;
        return Result.Success;
    }

    public Result<Success> MarkAsRefunded()
    {
        if (Status == PaymentStatus.Refunded)
        {
            return Result.Success;
        }

        Status = PaymentStatus.Refunded;
        ProcessedAtUtc = DateTime.UtcNow;
        return Result.Success;
    }
}
