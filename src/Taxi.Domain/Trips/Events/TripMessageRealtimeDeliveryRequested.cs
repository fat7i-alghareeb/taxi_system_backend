using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

public sealed class TripMessageRealtimeDeliveryRequested : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid MessageId { get; init; }

    public Guid SenderId { get; init; }

    public TripMessageSenderRole SenderRole { get; init; }

    public string? Content { get; init; }

    public string? PhotoUrl { get; init; }

    public DateTimeOffset SentAtUtc { get; init; }

    public Guid PassengerId { get; init; }

    public Guid? DriverUserId { get; init; }
}
