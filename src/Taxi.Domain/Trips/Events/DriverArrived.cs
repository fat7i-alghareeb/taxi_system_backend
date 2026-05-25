using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

public sealed class DriverArrived : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid DriverId { get; init; }

    public Guid PassengerId { get; init; }
}
