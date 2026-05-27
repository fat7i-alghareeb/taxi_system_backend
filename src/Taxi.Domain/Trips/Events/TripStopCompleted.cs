using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

/// <summary>
/// Raised when the driver marks an intermediate stop as completed during a
/// multi-stop trip. Final dropoff completion is signalled by
/// <see cref="TripCompleted"/> instead.
/// </summary>
public sealed class TripStopCompleted : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid PassengerId { get; init; }

    public Guid? DriverId { get; init; }

    public int Sequence { get; init; }
}
