using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

/// <summary>
/// Raised when a trip's drop-off is changed mid-trip (destination edit) while a driver is
/// assigned. Drives the driver-facing "route changed" push + realtime re-route, and informs
/// the passenger / admins. Only raised when the final stop actually moved.
/// </summary>
public sealed class TripDestinationChanged : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid PassengerId { get; init; }

    public Guid? DriverId { get; init; }

    public string ReferenceCode { get; init; } = string.Empty;

    public decimal NewDropoffLatitude { get; init; }

    public decimal NewDropoffLongitude { get; init; }

    public string? NewDropoffLabel { get; init; }
}
