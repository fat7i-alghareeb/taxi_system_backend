using System.Collections.Concurrent;
using Taxi.Application.Common.Interfaces;

namespace Taxi.Infrastructure.RealTime;

/// <summary>
/// Caches the encoded driver→pickup route per trip so the high-frequency location
/// stream can include a ready road-following polyline without calling the Directions
/// API on every tick. The route is recomputed only once the driver has moved past
/// <see cref="RefreshDistanceMeters"/> or the cached entry is older than <see cref="MaxAge"/>.
/// Registered as a singleton.
/// </summary>
public sealed class PickupRouteCache
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(12);
    private const double RefreshDistanceMeters = 40d;

    private readonly ConcurrentDictionary<Guid, Entry> _entries = new();

    public async Task<string?> GetOrComputeAsync(
        Guid tripId,
        double driverLat,
        double driverLng,
        double pickupLat,
        double pickupLng,
        IDirectionsService directionsService)
    {
        if (_entries.TryGetValue(tripId, out var cached))
        {
            var moved = HaversineMeters(cached.AnchorLat, cached.AnchorLng, driverLat, driverLng);
            var age = DateTimeOffset.UtcNow - cached.ComputedAtUtc;
            if (moved < RefreshDistanceMeters && age < MaxAge)
            {
                return cached.Polyline;
            }
        }

        try
        {
            var response = await directionsService.GetDirectionsAsync(
                (decimal)driverLat,
                (decimal)driverLng,
                (decimal)pickupLat,
                (decimal)pickupLng);

            var polyline = response.EncodedPolyline;
            _entries[tripId] = new Entry(polyline, driverLat, driverLng, DateTimeOffset.UtcNow);
            return polyline;
        }
        catch
        {
            // Directions failed (quota/network) — reuse any stale route rather than
            // dropping the line entirely.
            return _entries.TryGetValue(tripId, out var stale) ? stale.Polyline : null;
        }
    }

    public void Remove(Guid tripId) => _entries.TryRemove(tripId, out _);

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000d;
        var dLat = (lat2 - lat1) * (Math.PI / 180d);
        var dLon = (lon2 - lon1) * (Math.PI / 180d);

        var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2)) +
                (Math.Cos(lat1 * (Math.PI / 180d)) * Math.Cos(lat2 * (Math.PI / 180d)) *
                 Math.Sin(dLon / 2) * Math.Sin(dLon / 2));

        return earthRadiusMeters * (2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
    }

    private sealed record Entry(
        string Polyline,
        double AnchorLat,
        double AnchorLng,
        DateTimeOffset ComputedAtUtc);
}
