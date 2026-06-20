using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

public sealed class AdminAcceptedTrip : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid PassengerId { get; init; }

    public Guid AdminId { get; init; }

    public DateTimeOffset? ScheduledAtUtc { get; init; }
}
