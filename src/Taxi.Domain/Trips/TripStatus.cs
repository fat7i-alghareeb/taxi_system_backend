namespace Taxi.Domain.Trips;

public enum TripStatus
{
    PendingQuote,
    Scheduled,
    PendingDriver,
    DriverAssigned,
    DriverEnRoute,
    DriverArrived,
    InProgress,
    Completed,
    Cancelled,
    AwaitingPayment,
    PaymentFailed,
    Refunded
}

