namespace Taxi.Domain.Payments;

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}

public enum PaymentMethod
{
    Cash,
    CreditCard,
    Wallet
}

public enum PaymentKind
{
    /// <summary>The main upfront trip fare.</summary>
    Fare,

    /// <summary>An off-session waiting-fee surcharge charged after the trip completes.</summary>
    WaitingFee,

    /// <summary>
    /// An extra charge for a fare increase after a mid-trip edit (new destination /
    /// bigger vehicle). Settled wallet-first → default saved card off-session →
    /// PaymentSheet fallback, like a waiting-fee surcharge.
    /// </summary>
    FareAdjustment
}

