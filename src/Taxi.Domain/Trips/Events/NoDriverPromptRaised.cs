using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

/// <summary>
/// Raised when a trip has sat in <see cref="TripStatus.AwaitingAdminAcceptance"/>
/// past its no-driver deadline without being accepted. Drives the customer-facing
/// "no driver found — postpone or cancel" prompt (SignalR + FCM).
/// </summary>
public sealed class NoDriverPromptRaised : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid PassengerId { get; init; }

    public Guid VehicleTypeId { get; init; }

    public string ReferenceCode { get; init; } = string.Empty;

    public DateTimeOffset? ScheduledAtUtc { get; init; }
}
