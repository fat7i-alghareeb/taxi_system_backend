using Microsoft.AspNetCore.SignalR;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Notifications;
using Taxi.Infrastructure.Hubs;

namespace Taxi.Infrastructure.RealTime;

/// <summary>
/// SignalR implementation of ITripNotifier.
/// Pushes trip lifecycle events to connected clients using targeted groups.
/// Payloads are strongly-typed records from <c>Taxi.Contracts.Notifications</c>.
/// </summary>
public sealed class SignalRTripNotifier(IHubContext<TripHub> hubContext) : ITripNotifier
{
    private readonly IHubContext<TripHub> _hubContext = hubContext;

    public Task NotifyTripRequestedAsync(Guid tripId, Guid vehicleTypeId, Guid passengerId, CancellationToken ct = default) =>
        _hubContext.Clients
            .Group($"VehicleType_{vehicleTypeId}")
            .SendAsync("TripRequested", new TripRequestedNotification(tripId, vehicleTypeId, passengerId), ct);

    public Task NotifyDriverAssignedAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default) =>
        _hubContext.Clients
            .Group($"Trip_{tripId}")
            .SendAsync("DriverAssigned", new DriverAssignedNotification(tripId, passengerId, driverId), ct);

    public Task NotifyDriverEnRouteAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default) =>
        _hubContext.Clients
            .Group($"Trip_{tripId}")
            .SendAsync("DriverEnRoute", new DriverEnRouteNotification(tripId, passengerId, driverId), ct);

    public Task NotifyDriverArrivedAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default) =>
        _hubContext.Clients
            .Group($"Trip_{tripId}")
            .SendAsync("DriverArrived", new DriverArrivedNotification(tripId, passengerId, driverId), ct);

    public Task NotifyTripStartedAsync(Guid tripId, Guid passengerId, CancellationToken ct = default) =>
        _hubContext.Clients
            .Group($"Trip_{tripId}")
            .SendAsync("TripStarted", new TripStartedNotification(tripId, passengerId), ct);

    public Task NotifyTripCompletedAsync(Guid tripId, Guid passengerId, CancellationToken ct = default) =>
        _hubContext.Clients
            .Group($"Trip_{tripId}")
            .SendAsync("TripCompleted", new TripCompletedNotification(tripId, passengerId), ct);

    public Task NotifyTripCancelledAsync(Guid tripId, Guid passengerId, CancellationToken ct = default) =>
        _hubContext.Clients
            .Group($"Trip_{tripId}")
            .SendAsync("TripCancelled", new TripCancelledNotification(tripId, passengerId), ct);

    public Task NotifyPaymentConfirmedAsync(Guid tripId, Guid passengerId, CancellationToken ct = default) =>
        _hubContext.Clients
            .Group($"Trip_{tripId}")
            .SendAsync("PaymentConfirmed", new PaymentConfirmedNotification(tripId, passengerId), ct);

    public Task NotifyPaymentFailedAsync(Guid tripId, Guid passengerId, string reason, CancellationToken ct = default) =>
        _hubContext.Clients
            .Group($"Trip_{tripId}")
            .SendAsync("PaymentFailed", new PaymentFailedNotification(tripId, passengerId, reason), ct);

    public Task NotifyTripRefundedAsync(Guid tripId, Guid passengerId, decimal amount, CancellationToken ct = default) =>
        _hubContext.Clients
            .Group($"Trip_{tripId}")
            .SendAsync("TripRefunded", new TripRefundedNotification(tripId, passengerId, amount), ct);
}
