using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

public sealed class PaymentConfirmed : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid PassengerId { get; init; }
}
