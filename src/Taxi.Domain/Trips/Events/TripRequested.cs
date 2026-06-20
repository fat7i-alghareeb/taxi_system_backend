using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

public sealed class TripRequested : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid VehicleTypeId { get; init; }

    public Guid PassengerId { get; init; }

    /// <summary>Legacy compatibility flag. Scheduled activation is no longer used.</summary>
    public bool WasScheduled { get; init; }
}

