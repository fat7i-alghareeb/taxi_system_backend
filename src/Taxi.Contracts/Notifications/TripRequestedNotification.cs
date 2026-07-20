namespace Taxi.Contracts.Notifications;

/// <summary>
/// Strongly-typed payloads pushed over SignalR via <c>TripHub</c>.
/// The record name is the hub method name on the client (e.g. <c>"TripRequested"</c>).
/// Keep these in sync with the Flutter <c>RealtimeEvent</c> union.
/// </summary>
public sealed record TripRequestedNotification(
    Guid TripId,
    Guid VehicleTypeId,
    Guid PassengerId,
    Guid EventId = default);

public sealed record TripAwaitingAdminAcceptanceNotification(
    Guid TripId,
    Guid VehicleTypeId,
    Guid PassengerId,
    DateTimeOffset? ScheduledAtUtc,
    Guid EventId = default);

public sealed record AdminAcceptedTripNotification(
    Guid TripId,
    Guid PassengerId,
    Guid AdminId,
    Guid EventId = default);

public sealed record DriverAssignedNotification(
    Guid TripId,
    Guid PassengerId,
    Guid DriverId,
    Guid EventId = default);

public sealed record DriverEnRouteNotification(
    Guid TripId,
    Guid PassengerId,
    Guid DriverId,
    Guid EventId = default);

public sealed record DriverArrivedNotification(
    Guid TripId,
    Guid PassengerId,
    Guid DriverId,
    Guid EventId = default);

public sealed record TripStartedNotification(
    Guid TripId,
    Guid PassengerId,
    Guid EventId = default);

public sealed record TripCompletedNotification(
    Guid TripId,
    Guid PassengerId,
    Guid EventId = default);

public sealed record TripCancelledNotification(
    Guid TripId,
    Guid PassengerId,
    Guid EventId = default);

public sealed record PaymentConfirmedNotification(
    Guid TripId,
    Guid PassengerId,
    Guid EventId = default);

public sealed record PaymentFailedNotification(
    Guid TripId,
    Guid PassengerId,
    string Reason,
    Guid EventId = default);

public sealed record TripRefundedNotification(
    Guid TripId,
    Guid PassengerId,
    decimal Amount,
    Guid EventId = default);

public sealed record RefundLifecycleChangedNotification(
    Guid RefundId,
    Guid PaymentId,
    Guid? TripId,
    Guid? PassengerId,
    string Status,
    decimal Amount,
    string Currency,
    bool RequiresAdminAction,
    bool CanRetry,
    string SourceType,
    Guid EventId = default);

public sealed record RefundIssueCreatedNotification(
    Guid RefundIssueId,
    Guid TripId,
    Guid PassengerId,
    Guid? PaymentId,
    string RequestType,
    string ReviewStatus,
    Guid EventId = default);

public sealed record TripStopCompletedNotification(
    Guid TripId,
    Guid PassengerId,
    Guid? DriverId,
    int Sequence,
    Guid EventId = default);

public sealed record NoDriverFoundNotification(
    Guid TripId,
    Guid PassengerId,
    Guid EventId = default);

public sealed record TripDestinationChangedNotification(
    Guid TripId,
    Guid PassengerId,
    Guid? DriverId,
    decimal NewDropoffLatitude,
    decimal NewDropoffLongitude,
    string? NewDropoffLabel,
    Guid EventId = default);

/// <summary>
/// The customer's wallet balance moved. Sent when a fee is charged to debt, so the app can show
/// the outstanding amount immediately instead of waiting for the customer to open the wallet
/// screen and discover they are blocked from booking.
/// <c>AmountOwed</c> is positive when <c>Balance</c> is negative, zero otherwise.
/// </summary>
public sealed record WalletBalanceChangedNotification(
    Guid UserId,
    decimal Balance,
    decimal AmountOwed,
    string CurrencyCode,
    Guid EventId = default);

/// <summary>
/// A re-priced customer edit has been committed. <c>Delta</c> is what the change cost
/// (positive = charged, negative = refunded), so the app can confirm the amount immediately
/// while it refetches the full trip.
/// </summary>
public sealed record TripEditAppliedNotification(
    Guid TripId,
    Guid PassengerId,
    Guid? DriverId,
    decimal NewFare,
    string CurrencyCode,
    decimal Delta,
    int PassengerCount,
    string? VehicleTypeName,
    string? DropoffLabel,
    bool StopsChanged,
    bool PassengerCountChanged,
    Guid EventId = default);
