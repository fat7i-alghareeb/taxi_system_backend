using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

public sealed class ScheduledTripAdminReminder : DomainEvent
{
    public Guid TripId { get; init; }

    public string ReferenceCode { get; init; } = string.Empty;

    public DateTimeOffset ScheduledAtUtc { get; init; }

    public ScheduledTripReminderStage Stage { get; init; }
}
