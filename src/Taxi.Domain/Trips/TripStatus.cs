namespace Taxi.Domain.Trips;

public enum TripStatus
{
    PendingQuote,
    PendingDriver,
    DriverAssigned,
    InProgress,
    Completed,
    Cancelled
}
