using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips;
using Taxi.Infrastructure.RealTime;

namespace Taxi.Infrastructure.Hubs;

[Authorize]
public sealed class LocationTrackingHub(
    IAppDbContext context,
    IDriverLocationNotifier locationNotifier,
    IDirectionsService directionsService,
    PickupRouteCache pickupRouteCache) : Hub
{
    /// <summary>Average urban driving speed used to derive the driver-arrival ETA (~30 km/h).</summary>
    private const double AverageSpeedMetersPerSecond = 8.33;

    private readonly IAppDbContext _context = context;
    private readonly IDriverLocationNotifier _locationNotifier = locationNotifier;
    private readonly IDirectionsService _directionsService = directionsService;
    private readonly PickupRouteCache _pickupRouteCache = pickupRouteCache;

    public async Task UpdateLocation(double lat, double lng)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return;
        }

        // The acting user may be a registered Driver OR an admin operating as the driver.
        // Admins accept trips via Trip.AcceptedByAdminId and have no Driver row.
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userGuid);

        // Only broadcast to the customer once the driver is actually moving toward them
        // (EnRoute) or beyond. "Accepted" stays hidden so the car appears only when moving.
        var activeTrip = await _context.Trips
            .Include(t => t.Stops)
            .FirstOrDefaultAsync(t =>
                ((driver != null && t.DriverId == driver.Id) || t.AcceptedByAdminId == userGuid) &&
                (t.Status == TripStatus.EnRoute ||
                 t.Status == TripStatus.Arrived ||
                 t.Status == TripStatus.InProgress));

        // Persist the latest coordinate on the Driver entity when one exists (admins have none).
        if (driver != null)
        {
            var updateResult = driver.UpdateLocation((decimal)lat, (decimal)lng);
            if (updateResult.IsSuccess)
            {
                await _context.SaveChangesAsync(default);
            }
        }

        // Identify the moving party to the customer: Driver.Id for drivers, admin user id otherwise.
        var broadcastDriverId = driver?.Id ?? userGuid;
        var statusText = driver?.Status.ToString() ?? "OnTrip";

        var (etaSeconds, distanceMeters) = ResolveArrivalEstimate(activeTrip, lat, lng);

        // Road-following driver→target route (encoded), ready for the client to draw
        // immediately — computed via Directions but cached/throttled to avoid a call
        // per tick. Targets the pickup while heading there (EnRoute/Arrived) and the
        // destination once the passenger is on board (InProgress).
        var routePolyline = await ResolveTargetRouteAsync(activeTrip, lat, lng);

        await _locationNotifier.NotifyLocationUpdatedAsync(
            broadcastDriverId,
            lat,
            lng,
            statusText,
            activeTrip?.Id,
            etaSeconds,
            distanceMeters,
            routePolyline,
            Context.ConnectionAborted);
    }

    /// <summary>
    /// Returns the encoded driver→target route, reusing a throttled per-trip cache.
    /// The target is the pickup (first stop) while heading there (EnRoute/Arrived) and
    /// the destination (last stop) once the passenger is on board (InProgress).
    /// Clears the cache for any other status.
    /// </summary>
    private async Task<string?> ResolveTargetRouteAsync(Trip? trip, double lat, double lng)
    {
        if (trip is null)
        {
            return null;
        }

        var target = ResolveTargetStop(trip);
        if (target is null)
        {
            _pickupRouteCache.Remove(trip.Id);
            return null;
        }

        return await _pickupRouteCache.GetOrComputeAsync(
            trip.Id,
            lat,
            lng,
            (double)target.Coordinate.Latitude,
            (double)target.Coordinate.Longitude,
            _directionsService);
    }

    /// <summary>
    /// Computes the live arrival estimate from the driver's current position to the
    /// current target stop: the pickup (first stop) while heading there (EnRoute/Arrived)
    /// and the destination (last stop) once the passenger is on board (InProgress).
    /// </summary>
    private static (int? EtaSeconds, int? DistanceMeters) ResolveArrivalEstimate(
        Trip? trip,
        double lat,
        double lng)
    {
        if (trip is null)
        {
            return (null, null);
        }

        var target = ResolveTargetStop(trip);
        if (target is null)
        {
            return (null, null);
        }

        var distanceMeters = HaversineMeters(
            lat,
            lng,
            (double)target.Coordinate.Latitude,
            (double)target.Coordinate.Longitude);

        var etaSeconds = (int)Math.Round(distanceMeters / AverageSpeedMetersPerSecond);
        return (etaSeconds, (int)Math.Round(distanceMeters));
    }

    /// <summary>
    /// Resolves the stop the live estimate/route should target for the trip's status:
    /// the pickup (first stop) while EnRoute/Arrived, the destination (last stop) while
    /// InProgress, and null for any other status (no live tracking).
    /// </summary>
    private static TripStop? ResolveTargetStop(Trip trip)
    {
        var ordered = trip.Stops.OrderBy(s => s.Sequence);
        return trip.Status switch
        {
            TripStatus.EnRoute or TripStatus.Arrived => ordered.FirstOrDefault(),
            TripStatus.InProgress => ordered.LastOrDefault(),
            _ => null,
        };
    }

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000d;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2)) +
                (Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                 Math.Sin(dLon / 2) * Math.Sin(dLon / 2));

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180d);
}
