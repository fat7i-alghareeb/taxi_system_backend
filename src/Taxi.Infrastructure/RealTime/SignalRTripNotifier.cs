using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Notifications;
using Taxi.Infrastructure.Hubs;

namespace Taxi.Infrastructure.RealTime;

/// <summary>
/// SignalR implementation of ITripNotifier.
/// Pushes trip lifecycle events to connected clients using targeted groups.
/// Payloads are strongly-typed records from <c>Taxi.Contracts.Notifications</c>.
/// </summary>
/// <remarks>
/// Passenger-facing events are sent to BOTH the per-trip group <c>Trip_{tripId}</c>
/// (joined on demand while the active-trip screen is open) AND the per-user group
/// <c>User_{passengerId}</c> (always joined on connect in <see cref="TripHub.OnConnectedAsync"/>).
/// The per-user channel guarantees delivery to the passenger's connection even if the
/// app never joined the trip group — which is what lets the client open the rating sheet
/// globally on completion.
/// </remarks>
public sealed class SignalRTripNotifier(IHubContext<TripHub> hubContext, ILogger<SignalRTripNotifier> logger)
    : ITripNotifier
{
    private readonly IHubContext<TripHub> _hubContext = hubContext;
    private readonly ILogger<SignalRTripNotifier> _logger = logger;

    public Task NotifyTripRequestedAsync(Guid tripId, Guid vehicleTypeId, Guid passengerId, CancellationToken ct = default)
    {
        var payload = new TripRequestedNotification(tripId, vehicleTypeId, passengerId);

        _logger.LogInformation(
            "[SignalRTripNotifier] => TripRequested groups=VehicleType_{VehicleTypeId},Admins tripId={TripId} passengerId={PassengerId}",
            vehicleTypeId,
            tripId,
            passengerId);

        // Notify the matching drivers (subscribed to this vehicle type) AND every admin.
        return Task.WhenAll(
            _hubContext.Clients.Group($"VehicleType_{vehicleTypeId}").SendAsync("TripRequested", payload, ct),
            _hubContext.Clients.Group("Admins").SendAsync("TripRequested", payload, ct));
    }

    public Task NotifyDriverAssignedAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default) =>
        SendToPassengerAsync("DriverAssigned", tripId, passengerId, new DriverAssignedNotification(tripId, passengerId, driverId), ct);

    public Task NotifyDriverEnRouteAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default) =>
        SendToPassengerAsync("DriverEnRoute", tripId, passengerId, new DriverEnRouteNotification(tripId, passengerId, driverId), ct);

    public Task NotifyDriverArrivedAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default) =>
        SendToPassengerAsync("DriverArrived", tripId, passengerId, new DriverArrivedNotification(tripId, passengerId, driverId), ct);

    public Task NotifyTripStartedAsync(Guid tripId, Guid passengerId, CancellationToken ct = default) =>
        SendToPassengerAsync("TripStarted", tripId, passengerId, new TripStartedNotification(tripId, passengerId), ct);

    public Task NotifyTripCompletedAsync(Guid tripId, Guid passengerId, CancellationToken ct = default) =>
        SendToPassengerAsync("TripCompleted", tripId, passengerId, new TripCompletedNotification(tripId, passengerId), ct);

    public Task NotifyTripCancelledAsync(Guid tripId, Guid passengerId, CancellationToken ct = default) =>
        SendToPassengerAsync("TripCancelled", tripId, passengerId, new TripCancelledNotification(tripId, passengerId), ct);

    public Task NotifyPaymentConfirmedAsync(Guid tripId, Guid passengerId, CancellationToken ct = default) =>
        SendToPassengerAsync("PaymentConfirmed", tripId, passengerId, new PaymentConfirmedNotification(tripId, passengerId), ct);

    public Task NotifyPaymentFailedAsync(Guid tripId, Guid passengerId, string reason, CancellationToken ct = default) =>
        SendToPassengerAsync("PaymentFailed", tripId, passengerId, new PaymentFailedNotification(tripId, passengerId, reason), ct);

    public Task NotifyTripRefundedAsync(Guid tripId, Guid passengerId, decimal amount, CancellationToken ct = default) =>
        SendToPassengerAsync("TripRefunded", tripId, passengerId, new TripRefundedNotification(tripId, passengerId, amount), ct);

    public Task NotifyTripStopCompletedAsync(Guid tripId, Guid passengerId, Guid? driverId, int sequence, CancellationToken ct = default) =>
        SendToPassengerAsync("TripStopCompleted", tripId, passengerId, new TripStopCompletedNotification(tripId, passengerId, driverId, sequence), ct);

    /// <summary>
    /// Sends a passenger-facing event to both the per-trip group and the passenger's
    /// always-on per-user group, logging the emit and target groups first.
    /// </summary>
    private Task SendToPassengerAsync(string eventName, Guid tripId, Guid passengerId, object payload, CancellationToken ct)
    {
        _logger.LogInformation(
            "[SignalRTripNotifier] => {EventName} groups=Trip_{TripId},User_{PassengerId} tripId={TripId} passengerId={PassengerId}",
            eventName,
            tripId,
            passengerId,
            tripId,
            passengerId);

        return Task.WhenAll(
            _hubContext.Clients.Group($"Trip_{tripId}").SendAsync(eventName, payload, ct),
            _hubContext.Clients.Group($"User_{passengerId}").SendAsync(eventName, payload, ct));
    }
}
