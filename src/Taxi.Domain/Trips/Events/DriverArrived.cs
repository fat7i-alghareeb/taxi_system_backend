using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

public sealed class DriverArrived : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid DriverId { get; init; }

    public Guid PassengerId { get; init; }

    /// <summary>
    /// True when the driver marked arrival before the scheduled pickup time.
    /// Drives a clearer "arrived early" notification so the passenger knows they
    /// don't have to board yet and their free waiting window starts at the booked time.
    /// </summary>
    public bool IsEarlyArrival { get; init; }
}
