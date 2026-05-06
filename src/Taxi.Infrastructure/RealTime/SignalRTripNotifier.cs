using Microsoft.AspNetCore.SignalR;

using Taxi.Application.Common.Interfaces;
using Taxi.Infrastructure.Hubs;

namespace Taxi.Infrastructure.RealTime;

/// <summary>
/// SignalR implementation of ITripNotifier.
/// Pushes trip lifecycle events to connected clients using targeted groups,
/// following the same pattern as SignalRWorkOrderNotifier in the reference project.
/// </summary>
public sealed class SignalRTripNotifier(IHubContext<TripHub> hubContext) : ITripNotifier
{
    private readonly IHubContext<TripHub> _hubContext = hubContext;

    // Drivers subscribed to "VehicleType_{code}" receive new trip requests
    public Task NotifyTripRequestedAsync(Guid tripId, Guid vehicleTypeId, Guid passengerId, CancellationToken ct = default) =>
        _hubContext.Clients
            .Group($"VehicleType_{vehicleTypeId}")
            .SendAsync("TripRequested", new { tripId, vehicleTypeId, passengerId }, ct);

    // Passenger subscribed to "Trip_{tripId}" is notified their driver is confirmed
    public Task NotifyDriverAssignedAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default) =>
        _hubContext.Clients
            .Group($"Trip_{tripId}")
            .SendAsync("DriverAssigned", new { tripId, passengerId, driverId }, ct);

    // Both passenger and driver subscribed to "Trip_{tripId}" are notified
    public Task NotifyTripStartedAsync(Guid tripId, Guid passengerId, CancellationToken ct = default) =>
        _hubContext.Clients
            .Group($"Trip_{tripId}")
            .SendAsync("TripStarted", new { tripId, passengerId }, ct);

    public Task NotifyTripCompletedAsync(Guid tripId, Guid passengerId, CancellationToken ct = default) =>
        _hubContext.Clients
            .Group($"Trip_{tripId}")
            .SendAsync("TripCompleted", new { tripId, passengerId }, ct);

    public Task NotifyTripCancelledAsync(Guid tripId, Guid passengerId, CancellationToken ct = default) =>
        _hubContext.Clients
            .Group($"Trip_{tripId}")
            .SendAsync("TripCancelled", new { tripId, passengerId }, ct);
}
