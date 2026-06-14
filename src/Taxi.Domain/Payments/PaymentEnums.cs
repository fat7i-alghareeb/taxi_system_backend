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
    WaitingFee
}

