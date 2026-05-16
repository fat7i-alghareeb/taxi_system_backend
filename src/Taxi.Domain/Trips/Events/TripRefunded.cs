using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

public sealed class TripRefunded : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid PassengerId { get; init; }

    public decimal Amount { get; init; }
}
