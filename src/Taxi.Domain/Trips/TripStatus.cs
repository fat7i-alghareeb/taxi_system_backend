namespace Taxi.Domain.Trips;

public enum TripStatus
{
    PendingQuote,
    AwaitingAdminAcceptance,
    Accepted,
    EnRoute,
    Arrived,
    InProgress,
    Completed,
    Cancelled,
    AwaitingPayment,
    PaymentFailed,
    Refunded
}

