using Microsoft.AspNetCore.SignalR;
using Taxi.Application.Common.Interfaces;
using Taxi.Infrastructure.Hubs;

namespace Taxi.Infrastructure.RealTime;

public sealed class SignalRDriverLocationNotifier(IHubContext<TripHub> hubContext)
    : IDriverLocationNotifier
{
    public Task NotifyLocationUpdatedAsync(
        Guid driverId,
        double latitude,
        double longitude,
        string status,
        Guid? activeTripId,
        int? etaToPickupSeconds,
        int? distanceToPickupMeters,
        string? routeToPickupPolyline,
        CancellationToken ct = default)
    {
        var payload = new
        {
            EventId = Guid.NewGuid(),
            EventName = "DriverLocationUpdated",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            TripId = activeTripId,
            DriverId = driverId,
            Latitude = latitude,
            Longitude = longitude,
            Status = status,
            EtaToPickupSeconds = etaToPickupSeconds,
            DistanceToPickupMeters = distanceToPickupMeters,
            RouteToPickupPolyline = routeToPickupPolyline,
        };

        var sends = new List<Task>
        {
            hubContext.Clients.Group(TripHub.AdminsGroup)
                .SendAsync("DriverLocationUpdated", payload, ct),
        };

        if (activeTripId is { } tripId)
        {
            sends.Add(
                hubContext.Clients.Group($"Trip_{tripId}")
                    .SendAsync("DriverLocationUpdated", payload, ct));
        }

        return Task.WhenAll(sends);
    }
}
