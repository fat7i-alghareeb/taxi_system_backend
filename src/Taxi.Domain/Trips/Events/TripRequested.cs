using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

public sealed class TripRequested : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid VehicleTypeId { get; init; }

    public Guid PassengerId { get; init; }
}
