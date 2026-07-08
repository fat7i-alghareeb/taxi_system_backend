namespace Taxi.Domain.Wallet;

/// <summary>
/// The business reason a <see cref="WalletTransaction"/> exists. The full set is
/// declared up front so later phases (trip payment, fee charging, refunds, admin
/// adjustments) add no enum/migration churn; Phase 1 only produces <see cref="TopUp"/>.
/// </summary>
public enum WalletTransactionType
{
    /// <summary>Customer added money to the wallet via Stripe (credited on webhook success).</summary>
    TopUp,

    /// <summary>Wallet used to pay (part of) a trip fare. [Later phase]</summary>
    TripPayment,

    /// <summary>Wallet used to settle a ride-related fee (waiting/cancellation/extra). [Later phase]</summary>
    FeeCharge,

    /// <summary>Wallet-funded portion of a refund reversed back to the wallet. [Later phase]</summary>
    RefundReversal,

    /// <summary>Manual balance adjustment made by an admin with an audit reason. [Later phase]</summary>
    AdminAdjustment,

    /// <summary>Reconciliation/correction entry. [Later phase]</summary>
    Correction,
}

public enum WalletTransactionDirection
{
    /// <summary>Increases the wallet balance.</summary>
    Credit,

    /// <summary>Decreases the wallet balance.</summary>
    Debit,
}

public enum WalletTransactionStatus
{
    /// <summary>Recorded but not yet applied to the balance (e.g. a top-up awaiting Stripe confirmation).</summary>
    Pending,

    /// <summary>Applied to the balance. Only committed entries count toward the materialized balance.</summary>
    Committed,

    /// <summary>A hold/reservation that was released without affecting the balance. [Later phase]</summary>
    Released,

    /// <summary>The underlying operation failed; the entry never affected the balance. [Later phase]</summary>
    Failed,
}
