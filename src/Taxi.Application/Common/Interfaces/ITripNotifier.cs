namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Port interface that decouples the Application layer from the SignalR infrastructure.
/// Implemented by SignalRTripNotifier in the Infrastructure layer.
/// </summary>
public interface ITripNotifier
{
    /// <summary>Broadcasts to all drivers subscribed to the given vehicle type group.</summary>
    Task NotifyTripRequestedAsync(Guid tripId, Guid vehicleTypeId, Guid passengerId, CancellationToken ct = default);

    /// <summary>Sends to the specific passenger that their driver has been confirmed.</summary>
    Task NotifyDriverAssignedAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default);

    /// <summary>Sends to the trip group that the driver is en route.</summary>
    Task NotifyDriverEnRouteAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default);

    /// <summary>Sends to the trip group that the driver has arrived.</summary>
    Task NotifyDriverArrivedAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default);

    /// <summary>Sends to the trip group that the ride has started.</summary>
    Task NotifyTripStartedAsync(Guid tripId, Guid passengerId, CancellationToken ct = default);

    /// <summary>Sends to the trip group that the ride has been completed.</summary>
    Task NotifyTripCompletedAsync(Guid tripId, Guid passengerId, CancellationToken ct = default);

    /// <summary>Sends to the trip group that the ride has been cancelled.</summary>
    Task NotifyTripCancelledAsync(Guid tripId, Guid passengerId, CancellationToken ct = default);

    /// <summary>Sends to the passenger that their payment has been confirmed and the trip is being dispatched.</summary>
    Task NotifyPaymentConfirmedAsync(Guid tripId, Guid passengerId, CancellationToken ct = default);

    /// <summary>Sends to the passenger that their payment failed; the trip is now in PaymentFailed state.</summary>
    Task NotifyPaymentFailedAsync(Guid tripId, Guid passengerId, string reason, CancellationToken ct = default);

    /// <summary>Sends to the trip group that the trip has been refunded.</summary>
    Task NotifyTripRefundedAsync(Guid tripId, Guid passengerId, decimal amount, CancellationToken ct = default);
}

