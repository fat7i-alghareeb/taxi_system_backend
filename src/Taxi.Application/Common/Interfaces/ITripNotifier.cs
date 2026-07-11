using Taxi.Contracts.Notifications;

namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Port interface that decouples the Application layer from the SignalR infrastructure.
/// Implemented by SignalRTripNotifier in the Infrastructure layer.
/// </summary>
public interface ITripNotifier
{
    /// <summary>Broadcasts to all drivers subscribed to the given vehicle type group.</summary>
    Task NotifyTripRequestedAsync(Guid tripId, Guid vehicleTypeId, Guid passengerId, CancellationToken ct = default, Guid? eventId = null);

    Task NotifyTripAwaitingAdminAcceptanceAsync(
        Guid tripId,
        Guid vehicleTypeId,
        Guid passengerId,
        DateTimeOffset? scheduledAtUtc,
        CancellationToken ct = default,
        Guid? eventId = null);

    Task NotifyAdminAcceptedAsync(
        Guid tripId,
        Guid passengerId,
        Guid adminId,
        CancellationToken ct = default,
        Guid? eventId = null);

    /// <summary>Sends to the specific passenger that their driver has been confirmed.</summary>
    Task NotifyDriverAssignedAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default, Guid? eventId = null);

    /// <summary>Sends to the trip group that the driver is en route.</summary>
    Task NotifyDriverEnRouteAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default, Guid? eventId = null);

    /// <summary>Sends to the trip group that the driver has arrived.</summary>
    Task NotifyDriverArrivedAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default, Guid? eventId = null);

    /// <summary>Sends to the trip group that the ride has started.</summary>
    Task NotifyTripStartedAsync(Guid tripId, Guid passengerId, Guid? driverUserId, CancellationToken ct = default, Guid? eventId = null);

    /// <summary>Sends to the trip group that the ride has been completed.</summary>
    Task NotifyTripCompletedAsync(Guid tripId, Guid passengerId, CancellationToken ct = default, Guid? eventId = null);

    /// <summary>Sends to the trip group that the ride has been cancelled.</summary>
    Task NotifyTripCancelledAsync(Guid tripId, Guid passengerId, CancellationToken ct = default, Guid? eventId = null);

    /// <summary>Notifies the assigned driver (via their per-user group) that the trip was cancelled,
    /// so they stop driving to / waiting at the pickup.</summary>
    Task NotifyTripCancelledToDriverAsync(Guid tripId, Guid driverUserId, Guid passengerId, CancellationToken ct = default, Guid? eventId = null);

    /// <summary>Notifies all admins that the trip was cancelled, so the dashboard updates live.</summary>
    Task NotifyTripCancelledToAdminsAsync(Guid tripId, Guid passengerId, CancellationToken ct = default, Guid? eventId = null);

    /// <summary>Sends to the passenger that their payment has been confirmed and the trip is being dispatched.</summary>
    Task NotifyPaymentConfirmedAsync(Guid tripId, Guid passengerId, CancellationToken ct = default, Guid? eventId = null);

    /// <summary>Sends to the passenger that their payment failed; the trip is now in PaymentFailed state.</summary>
    Task NotifyPaymentFailedAsync(Guid tripId, Guid passengerId, string reason, CancellationToken ct = default, Guid? eventId = null);

    /// <summary>Sends to the trip group that the trip has been refunded.</summary>
    Task NotifyTripRefundedAsync(Guid tripId, Guid passengerId, decimal amount, CancellationToken ct = default, Guid? eventId = null);

    /// <summary>Broadcasts the durable refund lifecycle state to admins and the related passenger/trip groups.</summary>
    Task NotifyRefundLifecycleChangedAsync(
        Guid refundId,
        Guid paymentId,
        Guid? tripId,
        Guid? passengerId,
        string status,
        decimal amount,
        string currency,
        bool requiresAdminAction,
        bool canRetry,
        string sourceType,
        CancellationToken ct = default,
        Guid? eventId = null);

    /// <summary>Notifies admins that a customer refund issue has been submitted for review.</summary>
    Task NotifyRefundIssueCreatedToAdminsAsync(
        Guid refundIssueId,
        Guid tripId,
        Guid passengerId,
        Guid? paymentId,
        string requestType,
        string reviewStatus,
        CancellationToken ct = default,
        Guid? eventId = null);

    /// <summary>
    /// Sends to the trip group that an intermediate stop has just been completed
    /// during a multi-stop trip. The passenger UI uses this to advance the
    /// route progress indicator; admins see it on the live fleet map.
    /// </summary>
    Task NotifyTripStopCompletedAsync(Guid tripId, Guid passengerId, Guid? driverId, int sequence, CancellationToken ct = default, Guid? eventId = null);

    /// <summary>
    /// Sends to the passenger that no driver was found for their pending trip, so
    /// the app shows the blocking "postpone or cancel" overlay.
    /// </summary>
    Task NotifyNoDriverFoundAsync(Guid tripId, Guid passengerId, CancellationToken ct = default, Guid? eventId = null);

    /// <summary>
    /// Notifies the assigned driver's per-user group (so the nav/map re-routes), plus the trip group,
    /// the passenger and all admins, that the trip's drop-off changed mid-trip.
    /// </summary>
    Task NotifyTripDestinationChangedAsync(
        Guid tripId,
        Guid passengerId,
        Guid? driverUserId,
        Guid? driverId,
        decimal newDropoffLatitude,
        decimal newDropoffLongitude,
        string? newDropoffLabel,
        CancellationToken ct = default,
        Guid? eventId = null);

    /// <summary>
    /// Pushes a new in-trip chat message to the trip group, the passenger's per-user
    /// group, the driver's per-user group (if assigned), and all admins.
    /// </summary>
    Task NotifyTripMessageAsync(TripMessageNotification message, Guid passengerId, Guid? driverUserId, CancellationToken ct = default);

    /// <summary>
    /// Notifies the trip's chat participants that the chat has been closed
    /// (trip completed or cancelled) so clients can lock the message input.
    /// </summary>
    Task NotifyChatClosedAsync(Guid tripId, Guid passengerId, Guid? driverUserId, CancellationToken ct = default);

    /// <summary>
    /// Notifies all admins that a new customer incident was recorded, so the
    /// dashboard incident feed updates live.
    /// </summary>
    Task NotifyCustomerIncidentRaisedToAdminsAsync(
        Guid incidentId,
        Guid passengerId,
        Guid? tripId,
        string type,
        string severity,
        CancellationToken ct = default,
        Guid? eventId = null);
}

