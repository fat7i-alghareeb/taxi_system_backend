namespace Taxi.Domain.Trips;

public enum TripStatus
{
    PendingQuote,
    Scheduled,
    PendingDriver,
    DriverAssigned,
    InProgress,
    Completed,
    Cancelled
}
