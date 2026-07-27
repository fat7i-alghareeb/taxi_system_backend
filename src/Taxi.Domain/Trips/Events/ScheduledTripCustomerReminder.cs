using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

/// <summary>
/// Raised when a countdown reminder for a scheduled trip becomes due for the
/// passenger. Kept separate from <see cref="ScheduledTripAdminReminder"/> so the
/// admin dispatch reminders and the customer-promised reminders can never be
/// delivered to the wrong audience.
/// </summary>
public sealed class ScheduledTripCustomerReminder : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid PassengerId { get; init; }

    public string ReferenceCode { get; init; } = string.Empty;

    public DateTimeOffset ScheduledAtUtc { get; init; }

    public ScheduledTripReminderStage Stage { get; init; }
}
