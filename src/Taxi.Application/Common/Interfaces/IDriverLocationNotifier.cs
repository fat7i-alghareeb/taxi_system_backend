namespace Taxi.Application.Common.Interfaces;

public interface IDriverLocationNotifier
{
    Task NotifyLocationUpdatedAsync(
        Guid driverId,
        double latitude,
        double longitude,
        string status,
        Guid? activeTripId,
        int? etaToPickupSeconds,
        int? distanceToPickupMeters,
        string? routeToPickupPolyline,
        CancellationToken ct = default);
}
