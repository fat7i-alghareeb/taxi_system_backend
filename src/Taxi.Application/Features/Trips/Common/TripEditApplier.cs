using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Common;

/// <summary>
/// Commits a re-priced edit onto a trip: repoints the stops / passenger count / vehicle,
/// swaps the live quote (marking it used), and rebuilds the <see cref="TripRoute"/> from the
/// new quote's route data. Shared by the synchronous apply handler and the Stripe webhook so
/// both directions apply the change identically. Does NOT call SaveChanges — the caller owns
/// the transaction. Does NOT move money.
/// </summary>
public interface ITripEditApplier
{
    Task<Result<Success>> ApplyAsync(
        Trip trip,
        PricingQuote newQuote,
        IReadOnlyList<TripStop>? newStops,
        int? newPassengerCount,
        Guid? newVehicleTypeId,
        CancellationToken ct = default);
}

public sealed class TripEditApplier(IAppDbContext context) : ITripEditApplier
{
    public async Task<Result<Success>> ApplyAsync(
        Trip trip,
        PricingQuote newQuote,
        IReadOnlyList<TripStop>? newStops,
        int? newPassengerCount,
        Guid? newVehicleTypeId,
        CancellationToken ct = default)
    {
        var oldDropoff = trip.DropoffStop?.Coordinate;

        if (newStops is { Count: > 0 })
        {
            var stopsResult = trip.UpdateStops(newStops);
            if (stopsResult.IsError)
            {
                return stopsResult.Errors;
            }

            // Tell the assigned driver to re-route only when the drop-off actually moved.
            var newDropoff = trip.DropoffStop?.Coordinate;
            if (trip.DriverId is not null && DropoffMoved(oldDropoff, newDropoff))
            {
                trip.RaiseDestinationChanged();
            }
        }

        if (newPassengerCount.HasValue)
        {
            var passengerResult = trip.UpdatePassengerCount(newPassengerCount.Value, newVehicleTypeId);
            if (passengerResult.IsError)
            {
                return passengerResult.Errors;
            }
        }

        trip.UpdateQuote(newQuote.Id, newVehicleTypeId);
        newQuote.MarkAsUsed();

        await RefreshRouteAsync(trip, newQuote, ct);
        return Result.Success;
    }

    private async Task RefreshRouteAsync(Trip trip, PricingQuote newQuote, CancellationToken ct)
    {
        var existingRoute = await context.TripRoutes.FirstOrDefaultAsync(r => r.TripId == trip.Id, ct);
        if (existingRoute is not null)
        {
            context.TripRoutes.Remove(existingRoute);
        }

        var totalDistanceMeters = (int)Math.Round(newQuote.TotalDistanceKm * 1000m);
        var totalDurationSeconds = (int)Math.Round(newQuote.TotalDurationMin * 60m);

        var routeResult = TripRoute.Create(
            Guid.NewGuid(),
            trip.Id,
            newQuote.EncodedOverviewPolyline ?? string.Empty,
            totalDistanceMeters,
            totalDurationSeconds,
            newQuote.RouteSegmentsJson);

        if (routeResult.IsSuccess)
        {
            context.TripRoutes.Add(routeResult.Value);
        }
    }

    private const decimal CoordinateEpsilon = 0.000001m;

    private static bool DropoffMoved(Domain.Trips.Coordinate? oldDropoff, Domain.Trips.Coordinate? newDropoff)
    {
        if (oldDropoff is null || newDropoff is null)
        {
            return oldDropoff is not null || newDropoff is not null;
        }

        return Math.Abs(oldDropoff.Latitude - newDropoff.Latitude) > CoordinateEpsilon
            || Math.Abs(oldDropoff.Longitude - newDropoff.Longitude) > CoordinateEpsilon;
    }
}
