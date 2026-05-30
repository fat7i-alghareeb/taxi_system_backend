namespace Taxi.Contracts.Notifications;

/// <summary>
/// Strongly-typed payloads pushed over SignalR via <c>TripHub</c>.
/// The record name is the hub method name on the client (e.g. <c>"TripRequested"</c>).
/// Keep these in sync with the Flutter <c>RealtimeEvent</c> union.
/// </summary>
public sealed record TripRequestedNotification(
    Guid TripId,
    Guid VehicleTypeId,
    Guid PassengerId);

public sealed record DriverAssignedNotification(
    Guid TripId,
    Guid PassengerId,
    Guid DriverId);

public sealed record DriverEnRouteNotification(
    Guid TripId,
    Guid PassengerId,
    Guid DriverId);

public sealed record DriverArrivedNotification(
    Guid TripId,
    Guid PassengerId,
    Guid DriverId);

public sealed record TripStartedNotification(
    Guid TripId,
    Guid PassengerId);

public sealed record TripCompletedNotification(
    Guid TripId,
    Guid PassengerId);

public sealed record TripCancelledNotification(
    Guid TripId,
    Guid PassengerId);

public sealed record PaymentConfirmedNotification(
    Guid TripId,
    Guid PassengerId);

public sealed record PaymentFailedNotification(
    Guid TripId,
    Guid PassengerId,
    string Reason);

public sealed record TripRefundedNotification(
    Guid TripId,
    Guid PassengerId,
    decimal Amount);

public sealed record TripStopCompletedNotification(
    Guid TripId,
    Guid PassengerId,
    Guid? DriverId,
    int Sequence);
