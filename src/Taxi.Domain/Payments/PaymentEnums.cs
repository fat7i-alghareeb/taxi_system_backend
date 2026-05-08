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

