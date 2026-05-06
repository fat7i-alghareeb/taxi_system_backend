using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

public sealed class TripCompleted : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid PassengerId { get; init; }

    public Guid? DriverId { get; init; }
}
