using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

public sealed class TripStarted : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid PassengerId { get; init; }
}

