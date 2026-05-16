using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

public sealed class PaymentFailed : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid PassengerId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
