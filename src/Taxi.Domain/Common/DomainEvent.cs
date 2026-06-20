using MediatR;

namespace Taxi.Domain.Common;

public abstract class DomainEvent : INotification
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

