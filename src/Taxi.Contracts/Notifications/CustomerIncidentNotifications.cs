namespace Taxi.Contracts.Notifications;

/// <summary>
/// Pushed over SignalR (hub method <c>"CustomerIncidentRaised"</c>) to the Admins
/// group whenever a new customer incident is recorded, so the dashboard incident
/// feed updates live. Keep in sync with the Flutter <c>RealtimeEvent</c> union.
/// </summary>
public sealed record CustomerIncidentRaisedNotification(
    Guid IncidentId,
    Guid PassengerId,
    Guid? TripId,
    string Type,
    string Severity,
    Guid EventId = default);
