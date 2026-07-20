using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Owns all wallet balance mutations. Reads (balance / ledger) are done directly by their
/// query handlers. Every mutation is transactional, idempotent (via a unique transaction
/// key) and concurrency-safe (optimistic token on the account).
/// </summary>
public interface IWalletService
{
    /// <summary>
    /// Records the Pending top-up ledger entry for a just-created Stripe PaymentIntent.
    /// The wallet is not credited here — only after the payment succeeds (see
    /// <see cref="CreditTopUpFromWebhookAsync"/>). Creates the wallet account on first use.
    /// </summary>
    Task<Result<Success>> CreatePendingTopUpAsync(
        Guid walletTransactionId,
        Guid userId,
        decimal amount,
        string currency,
        string stripePaymentIntentId,
        CancellationToken ct = default);

    /// <summary>
    /// Credits the wallet for a succeeded top-up PaymentIntent. Safe to call repeatedly:
    /// a duplicate webhook is a no-op (<see cref="WalletTopUpCreditStatus.AlreadyCredited"/>).
    /// Returns <see cref="WalletTopUpCreditStatus.NotAWalletTopUp"/> when the PaymentIntent is
    /// not a wallet top-up, so the Stripe webhook handler continues to its normal trip-payment path.
    /// </summary>
    Task<Result<WalletTopUpCreditOutcome>> CreditTopUpFromWebhookAsync(
        string stripePaymentIntentId,
        decimal? amountReceived,
        string? stripeChargeId,
        CancellationToken ct = default);

    /// <summary>
    /// Marks the wallet top-up ledger entry Failed for a failed/canceled Stripe PaymentIntent.
    /// Safe to call repeatedly: a duplicate failure webhook, or a failure webhook that arrives
    /// after the top-up was already credited by a prior success webhook, is a no-op.
    /// Returns <see cref="WalletTopUpFailStatus.NotAWalletTopUp"/> when the PaymentIntent is not
    /// a wallet top-up, so the Stripe webhook handler continues to its normal trip-payment path.
    /// </summary>
    Task<Result<WalletTopUpFailOutcome>> FailTopUpFromWebhookAsync(
        string stripePaymentIntentId,
        string? reason,
        CancellationToken ct = default);

    /// <summary>
    /// Debits the wallet toward a ride-related fee, up to the available balance (partial debits
    /// are expected — the remainder is charged elsewhere). Returns the amount actually debited
    /// (0 if no account/balance). Idempotent via <paramref name="idempotencyKey"/>: a retry returns
    /// the amount already debited without debiting again. Concurrency-safe.
    /// </summary>
    Task<Result<WalletDebitOutcome>> DebitForFeeAsync(
        Guid userId,
        Guid tripId,
        decimal amount,
        string currency,
        string description,
        string idempotencyKey,
        CancellationToken ct = default);

    /// <summary>
    /// Charges the FULL amount of an already-incurred fee that could not be collected any other
    /// way, taking the balance negative. The resulting debt blocks further booking until settled.
    /// Creates the wallet account if the customer has none, so the debt always has somewhere to
    /// live. Idempotent via <paramref name="idempotencyKey"/>; concurrency-safe.
    /// </summary>
    /// <remarks>
    /// Only for fees the customer cannot decline (waiting time). Never use for a trip fare — a ride
    /// must not be taken on credit; that path uses <see cref="DebitForFeeAsync"/>, which clamps.
    /// </remarks>
    Task<Result<WalletDebitOutcome>> ChargeUncollectableFeeAsync(
        Guid userId,
        Guid tripId,
        decimal amount,
        string currency,
        string description,
        string idempotencyKey,
        CancellationToken ct = default);

    /// <summary>
    /// An admin's manual correction to a balance — waiving a debt, or fixing a mis-charge.
    /// A positive <paramref name="amount"/> credits, a negative one debits (and may overdraw,
    /// since a correction can legitimately reinstate a debt). Records the reason on the ledger
    /// entry. Returns the resulting balance.
    /// </summary>
    Task<Result<decimal>> AdjustBalanceAsync(
        Guid userId,
        decimal amount,
        string currency,
        string reason,
        Guid adminId,
        string idempotencyKey,
        CancellationToken ct = default);

    /// <summary>
    /// Credits the wallet to reverse a previously wallet-funded amount (a refund of money that
    /// originally came from the wallet). Idempotent via <paramref name="idempotencyKey"/>.
    /// </summary>
    Task<Result<Success>> CreditRefundReversalAsync(
        Guid userId,
        Guid tripId,
        Guid paymentId,
        Guid paymentRefundId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken ct = default);

    /// <summary>
    /// Reserves wallet money toward a trip fare, up to the available balance (a Pending hold that
    /// immediately reduces the available balance so it cannot be double-spent). Returns the amount
    /// held. Idempotent via <paramref name="idempotencyKey"/>. Used by wallet-only and mixed trip
    /// payments; the hold is later committed (card succeeded) or released (card failed).
    /// </summary>
    Task<Result<WalletHoldOutcome>> TryHoldForTripAsync(
        Guid userId,
        Guid tripId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken ct = default);

    /// <summary>
    /// Finalizes the pending trip-fare hold(s) for a trip (the card portion succeeded). Returns the
    /// total committed amount. Idempotent — already-committed holds are counted, not re-applied.
    /// </summary>
    Task<Result<decimal>> CommitTripHoldAsync(Guid tripId, CancellationToken ct = default);

    /// <summary>
    /// Releases the pending trip-fare hold(s) for a trip (the card portion failed/was cancelled),
    /// restoring the reserved amount to the balance. Idempotent.
    /// </summary>
    Task<Result<Success>> ReleaseTripHoldAsync(Guid tripId, CancellationToken ct = default);
}

public sealed record WalletDebitOutcome(decimal DebitedAmount, Guid? WalletTransactionId);

public sealed record WalletHoldOutcome(decimal HeldAmount, Guid? HoldTransactionId);

public enum WalletTopUpCreditStatus
{
    /// <summary>The PaymentIntent is not a wallet top-up; the caller should handle it elsewhere.</summary>
    NotAWalletTopUp,

    /// <summary>The wallet was credited by this call.</summary>
    Credited,

    /// <summary>The top-up was already credited by a previous call (idempotent no-op).</summary>
    AlreadyCredited,
}

public sealed record WalletTopUpCreditOutcome(
    WalletTopUpCreditStatus Status,
    Guid UserId,
    decimal Amount,
    decimal NewBalance,
    string Currency);

public enum WalletTopUpFailStatus
{
    /// <summary>The PaymentIntent is not a wallet top-up; the caller should handle it elsewhere.</summary>
    NotAWalletTopUp,

    /// <summary>The transaction was marked Failed by this call.</summary>
    Failed,

    /// <summary>The transaction was already Failed by a previous call (idempotent no-op).</summary>
    AlreadyFailed,

    /// <summary>The transaction was already Committed by a prior success webhook; failure ignored (idempotent no-op).</summary>
    AlreadyCommitted,
}

public sealed record WalletTopUpFailOutcome(
    WalletTopUpFailStatus Status,
    Guid UserId,
    decimal Amount,
    string Currency);
