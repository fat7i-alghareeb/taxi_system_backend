using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

public sealed class TripRequested : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid VehicleTypeId { get; init; }

    public Guid PassengerId { get; init; }

    /// <summary>
    /// True when this event is raised by a scheduled trip becoming active
    /// (i.e. `ScheduledTripActivationService` flipped it from `Scheduled` to
    /// `PendingDriver`). Used by event handlers to additionally notify the
    /// passenger that their pre-booked trip just went live.
    /// </summary>
    public bool WasScheduled { get; init; }
}

