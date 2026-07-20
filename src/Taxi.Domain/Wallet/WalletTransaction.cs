using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Wallet;

/// <summary>
/// An append-only ledger entry against a <see cref="WalletAccount"/>. Every movement of
/// wallet money is one row, uniquely keyed by <see cref="IdempotencyKey"/> so a retried
/// operation (API retry, duplicate Stripe webhook) can never post the same entry twice.
/// Phase 1 produces only pending top-ups that are committed on Stripe webhook success.
/// </summary>
public sealed class WalletTransaction : AuditableEntity
{
    private WalletTransaction() { } // EF Core

    private WalletTransaction(
        Guid id,
        Guid walletAccountId,
        WalletTransactionType type,
        WalletTransactionDirection direction,
        decimal amount,
        string currency,
        string idempotencyKey)
        : base(id)
    {
        WalletAccountId = walletAccountId;
        Type = type;
        Direction = direction;
        Amount = amount;
        Currency = currency;
        IdempotencyKey = idempotencyKey;
        Status = WalletTransactionStatus.Pending;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid WalletAccountId { get; private set; }
    public WalletTransactionType Type { get; private set; }
    public WalletTransactionDirection Direction { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;

    /// <summary>Balance immediately after this entry was committed. Null while Pending.</summary>
    public decimal? BalanceAfter { get; private set; }

    public WalletTransactionStatus Status { get; private set; }
    public string? Description { get; private set; }

    // Optional links to the business object that caused the movement. Populated by later
    // phases; a top-up only carries the Stripe references below.
    public Guid? TripId { get; private set; }
    public Guid? PaymentId { get; private set; }
    public Guid? PaymentRefundId { get; private set; }
    public Guid? CreatedByAdminId { get; private set; }

    public string? StripePaymentIntentId { get; private set; }
    public string? StripeChargeId { get; private set; }

    /// <summary>Unique key guaranteeing exactly-once posting under retries. Enforced by a DB unique index.</summary>
    public string IdempotencyKey { get; private set; } = default!;

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    /// <summary>
    /// Creates the Pending ledger entry for a Stripe-funded wallet top-up. The balance is not
    /// affected until <see cref="MarkCommitted"/> runs on the <c>payment_intent.succeeded</c> webhook.
    /// </summary>
    public static Result<WalletTransaction> CreatePendingTopUp(
        Guid id,
        Guid walletAccountId,
        decimal amount,
        string currency,
        string stripePaymentIntentId,
        string idempotencyKey,
        string? description = null)
    {
        if (amount <= 0m)
        {
            return WalletErrors.InvalidAmount;
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return WalletErrors.CurrencyRequired;
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return WalletErrors.IdempotencyKeyRequired;
        }

        return new WalletTransaction(
            id,
            walletAccountId,
            WalletTransactionType.TopUp,
            WalletTransactionDirection.Credit,
            decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            currency.Trim().ToUpperInvariant(),
            idempotencyKey)
        {
            StripePaymentIntentId = stripePaymentIntentId,
            Description = description,
        };
    }

    /// <summary>
    /// Creates the Pending ledger entry for a wallet debit that settles (part of) a ride-related
    /// fee. Committed once the balance is actually reduced in the same unit of work.
    /// </summary>
    public static Result<WalletTransaction> CreateFeeCharge(
        Guid id,
        Guid walletAccountId,
        decimal amount,
        string currency,
        string idempotencyKey,
        Guid? tripId = null,
        Guid? paymentId = null,
        string? description = null)
    {
        var validation = ValidateCommon(amount, currency, idempotencyKey);
        if (validation is { } error)
        {
            return error;
        }

        return new WalletTransaction(
            id,
            walletAccountId,
            WalletTransactionType.FeeCharge,
            WalletTransactionDirection.Debit,
            decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            currency.Trim().ToUpperInvariant(),
            idempotencyKey)
        {
            TripId = tripId,
            PaymentId = paymentId,
            Description = description,
        };
    }

    /// <summary>
    /// Creates the Pending ledger entry for an admin's manual correction to a balance — waiving a
    /// debt, or fixing a mis-charge. <paramref name="direction"/> decides whether it credits or
    /// debits; the reason is recorded in <paramref name="description"/> and mirrored to the audit
    /// log by the caller.
    /// </summary>
    public static Result<WalletTransaction> CreateAdminAdjustment(
        Guid id,
        Guid walletAccountId,
        decimal amount,
        string currency,
        string idempotencyKey,
        WalletTransactionDirection direction,
        Guid createdByAdminId,
        string? description = null)
    {
        var validation = ValidateCommon(amount, currency, idempotencyKey);
        if (validation is { } error)
        {
            return error;
        }

        return new WalletTransaction(
            id,
            walletAccountId,
            WalletTransactionType.AdminAdjustment,
            direction,
            decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            currency.Trim().ToUpperInvariant(),
            idempotencyKey)
        {
            CreatedByAdminId = createdByAdminId,
            Description = description,
        };
    }

    /// <summary>
    /// Creates the Pending ledger entry for reversing a wallet-funded amount back to the wallet
    /// (a refund of money that originally came from the wallet — never from a card).
    /// </summary>
    public static Result<WalletTransaction> CreateRefundReversal(
        Guid id,
        Guid walletAccountId,
        decimal amount,
        string currency,
        string idempotencyKey,
        Guid? tripId = null,
        Guid? paymentId = null,
        Guid? paymentRefundId = null,
        string? description = null)
    {
        var validation = ValidateCommon(amount, currency, idempotencyKey);
        if (validation is { } error)
        {
            return error;
        }

        return new WalletTransaction(
            id,
            walletAccountId,
            WalletTransactionType.RefundReversal,
            WalletTransactionDirection.Credit,
            decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            currency.Trim().ToUpperInvariant(),
            idempotencyKey)
        {
            TripId = tripId,
            PaymentId = paymentId,
            PaymentRefundId = paymentRefundId,
            Description = description,
        };
    }

    /// <summary>
    /// Creates a Pending hold that reserves wallet money toward a trip fare (mixed / wallet-only
    /// payment). The reservation reduces the available balance immediately and is finalized by
    /// <see cref="MarkCommitted"/> when the card portion succeeds, or returned by
    /// <see cref="MarkReleased"/> if it fails.
    /// </summary>
    public static Result<WalletTransaction> CreateTripPaymentHold(
        Guid id,
        Guid walletAccountId,
        decimal amount,
        string currency,
        string idempotencyKey,
        Guid tripId,
        string? description = null)
    {
        var validation = ValidateCommon(amount, currency, idempotencyKey);
        if (validation is { } error)
        {
            return error;
        }

        return new WalletTransaction(
            id,
            walletAccountId,
            WalletTransactionType.TripPayment,
            WalletTransactionDirection.Debit,
            decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            currency.Trim().ToUpperInvariant(),
            idempotencyKey)
        {
            TripId = tripId,
            Description = description,
        };
    }

    /// <summary>
    /// Releases a Pending hold without applying it to the balance (the caller restores the
    /// reserved amount). Idempotent and only valid from Pending.
    /// </summary>
    public Result<Success> MarkReleased()
    {
        if (Status is WalletTransactionStatus.Released or WalletTransactionStatus.Committed)
        {
            return Result.Success;
        }

        Status = WalletTransactionStatus.Released;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success;
    }

    /// <summary>
    /// Marks a Pending entry (e.g. a top-up) as failed — the underlying Stripe PaymentIntent
    /// was declined/cancelled and never affected the balance. Idempotent: a second call
    /// (duplicate failure webhook, or a failure arriving after the entry was already committed
    /// by a prior success webhook) is a no-op and never overrides a terminal state.
    /// </summary>
    public Result<Success> MarkFailed(string? reason = null)
    {
        if (Status is WalletTransactionStatus.Failed or WalletTransactionStatus.Committed)
        {
            return Result.Success;
        }

        Status = WalletTransactionStatus.Failed;
        CompletedAtUtc = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(reason))
        {
            Description = reason;
        }

        return Result.Success;
    }

    private static Error? ValidateCommon(decimal amount, string currency, string idempotencyKey)
    {
        if (amount <= 0m)
        {
            return WalletErrors.InvalidAmount;
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return WalletErrors.CurrencyRequired;
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return WalletErrors.IdempotencyKeyRequired;
        }

        return null;
    }

    /// <summary>
    /// Applies this entry to the balance. Idempotent: a second call (e.g. a duplicate webhook)
    /// is a no-op success so the balance is never credited twice.
    /// </summary>
    public Result<Success> MarkCommitted(decimal balanceAfter, string? stripeChargeId = null)
    {
        if (Status == WalletTransactionStatus.Committed)
        {
            return Result.Success;
        }

        Status = WalletTransactionStatus.Committed;
        BalanceAfter = decimal.Round(balanceAfter, 2, MidpointRounding.AwayFromZero);
        CompletedAtUtc = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(stripeChargeId))
        {
            StripeChargeId = stripeChargeId;
        }

        return Result.Success;
    }
}
