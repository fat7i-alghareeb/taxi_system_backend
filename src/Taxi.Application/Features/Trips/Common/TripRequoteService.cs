using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;
using Taxi.Domain.Pricing;
using Taxi.Domain.Trips;
using Taxi.Domain.Vehicles;

using AppCoordinate = Taxi.Application.Common.Interfaces.Coordinate;

namespace Taxi.Application.Features.Trips.Common;

/// <summary>A proposed stop for a re-quote (new destination / route).</summary>
public sealed record TripEditStop(decimal Latitude, decimal Longitude, string? Label);

/// <summary>
/// The result of re-pricing a proposed edit. The new <see cref="PricingQuote"/> and
/// <see cref="TripStop"/>s are BUILT but NOT persisted / applied — the caller decides
/// whether to commit them (delta==0 / refund) or defer them (charge needs a sheet).
/// </summary>
public sealed record TripRequoteResult(
    PricingQuote NewQuote,
    IReadOnlyList<TripStop> NewStops,
    Guid? NewVehicleTypeId,
    decimal OldFinalFare,
    decimal NewFinalFare,
    decimal Delta,
    string CurrencyCode,
    string OverviewPolyline,
    string RouteSegmentsJson,
    int TotalDistanceMeters,
    int TotalDurationSeconds);

public interface ITripRequoteService
{
    /// <summary>
    /// Re-prices a proposed change (new stops and/or new passenger count) without
    /// mutating the trip. Passenger edits re-select the cheapest active vehicle that
    /// fits the new count (up OR down); a stops-only edit keeps the current vehicle.
    /// </summary>
    Task<Result<TripRequoteResult>> RequoteAsync(
        Trip trip,
        IReadOnlyList<TripEditStop>? newStops,
        int? newPassengerCount,
        CancellationToken ct = default);

    /// <summary>
    /// Rebuilds the requote result from a quote already produced by a preview, instead of
    /// routing and pricing again. This is what makes preview → apply deterministic: the
    /// directions provider returns slightly different distances between two calls seconds
    /// apart, which used to move the delta and trip the drift guard for no real reason.
    /// Fails with <c>EditDeltaChanged</c> when the quote is expired / already used / does
    /// not describe the edit being applied, so the app re-previews.
    /// </summary>
    Task<Result<TripRequoteResult>> ResolveFromPreviewAsync(
        Trip trip,
        Guid previewToken,
        IReadOnlyList<TripEditStop>? newStops,
        int? newPassengerCount,
        CancellationToken ct = default);
}

public sealed class TripRequoteService(
    IAppDbContext context,
    IDirectionsService directionsService) : ITripRequoteService
{
    public async Task<Result<TripRequoteResult>> RequoteAsync(
        Trip trip,
        IReadOnlyList<TripEditStop>? newStops,
        int? newPassengerCount,
        CancellationToken ct = default)
    {
        var effectiveStops = EffectiveStops(trip, newStops);

        if (effectiveStops.Count < 2)
        {
            return TripErrors.InvalidStops;
        }

        // Vehicle selection.
        VehicleType vehicleType;
        Guid? newVehicleTypeId = null;
        if (newPassengerCount.HasValue)
        {
            if (newPassengerCount.Value < 1)
            {
                return TripErrors.InvalidPassengerCount;
            }

            var suitable = await context.VehicleTypes
                .Where(v => v.IsActive && v.PassengerCapacity >= newPassengerCount.Value)
                .OrderBy(v => v.PassengerCapacity)
                .ThenBy(v => v.SortOrder)
                .FirstOrDefaultAsync(ct);
            if (suitable is null)
            {
                return TripErrors.NoSuitableVehicleForPassengerCount;
            }

            vehicleType = suitable;
            newVehicleTypeId = suitable.Id == trip.VehicleTypeId ? null : suitable.Id;
        }
        else
        {
            var current = await context.VehicleTypes.FirstOrDefaultAsync(v => v.Id == trip.VehicleTypeId, ct);
            if (current is null)
            {
                return TripErrors.VehicleTypeNotFound;
            }

            vehicleType = current;
        }

        var serviceCoords = effectiveStops
            .Select(s => new AppCoordinate(s.Latitude, s.Longitude))
            .ToList();
        var directionResponse = await directionsService.GetDirectionsAsync(serviceCoords);

        var totalDistanceKm = Math.Round(directionResponse.TotalDistanceMeters / 1000m, 3);
        var totalDurationMin = Math.Round(directionResponse.TotalDurationSeconds / 60m, 2);

        var configs = await context.AppConfigs
            .Where(c => c.Key == AppConfigKeys.TripDiscountPercent || c.Key == AppConfigKeys.Currency)
            .ToListAsync(ct);
        var discountConfig = configs.FirstOrDefault(c => c.Key == AppConfigKeys.TripDiscountPercent);
        var discountPercent = decimal.TryParse(discountConfig?.Value, out var d) && d > 0 ? d : 0m;
        var currencyConfig = configs.FirstOrDefault(c => c.Key == AppConfigKeys.Currency);
        var currencyCode = string.IsNullOrWhiteSpace(currencyConfig?.Value) ? "EUR" : currencyConfig!.Value;

        var originalFare = PricingService.CalculateFare(vehicleType, (double)totalDistanceKm, (double)totalDurationMin);
        var newFinalFare = discountPercent > 0
            ? Math.Round(originalFare * (1 - (discountPercent / 100)), 2)
            : originalFare;

        // Old fare = the live quote the trip currently points at (never Payment.Amount,
        // so sequential edits always delta against the current fare).
        var currentQuote = await context.PricingQuotes.FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);
        var oldFinalFare = currentQuote?.FinalFare ?? 0m;

        var routeSegmentsJson = JsonSerializer.Serialize(
            directionResponse.Legs.Select(leg => new
            {
                distanceMeters = leg.DistanceMeters,
                durationSeconds = leg.DurationSeconds,
                encodedPolyline = leg.EncodedPolyline,
                startLatitude = leg.StartCoordinate.Latitude,
                startLongitude = leg.StartCoordinate.Longitude,
                endLatitude = leg.EndCoordinate.Latitude,
                endLongitude = leg.EndCoordinate.Longitude,
            }).ToList());

        var domainCoords = effectiveStops
            .Select(s => new Domain.Trips.Coordinate(s.Latitude, s.Longitude))
            .ToList();

        var newQuoteResult = PricingQuote.Create(
            Guid.NewGuid(),
            trip.PassengerId,
            vehicleType.Id,
            totalDistanceKm,
            totalDurationMin,
            newFinalFare,
            originalFare,
            discountPercent,
            currencyCode,
            DateTime.UtcNow.AddMinutes(15),
            domainCoords,
            directionResponse.OverviewPolyline,
            routeSegmentsJson);
        if (newQuoteResult.IsFailure)
        {
            return newQuoteResult.Error;
        }

        var stopResults = effectiveStops
            .Select((s, index) => TripStop.Create(
                new Domain.Trips.Coordinate(s.Latitude, s.Longitude), index, s.Label))
            .ToList();
        if (stopResults.Any(r => r.IsFailure))
        {
            return stopResults.First(r => r.IsFailure).Error;
        }

        var newStopList = stopResults.Select(r => r.Value).ToList();
        var delta = Math.Round(newFinalFare - oldFinalFare, 2);

        return new TripRequoteResult(
            newQuoteResult.Value,
            newStopList,
            newVehicleTypeId,
            oldFinalFare,
            newFinalFare,
            delta,
            currencyCode,
            directionResponse.OverviewPolyline,
            routeSegmentsJson,
            (int)Math.Round(totalDistanceKm * 1000m),
            (int)Math.Round(totalDurationMin * 60m));
    }

    public async Task<Result<TripRequoteResult>> ResolveFromPreviewAsync(
        Trip trip,
        Guid previewToken,
        IReadOnlyList<TripEditStop>? newStops,
        int? newPassengerCount,
        CancellationToken ct = default)
    {
        var effectiveStops = EffectiveStops(trip, newStops);
        if (effectiveStops.Count < 2)
        {
            return TripErrors.InvalidStops;
        }

        var quote = await context.PricingQuotes.FirstOrDefaultAsync(q => q.Id == previewToken, ct);

        // A quote that is missing, someone else's, already spent, or stale can't be trusted to
        // price this edit. Treat all of them as drift so the client previews again.
        if (quote is null || quote.PassengerId != trip.PassengerId || quote.Used || quote.IsExpired())
        {
            return TripErrors.EditDeltaChanged;
        }

        // The token must describe the edit actually being applied — otherwise a cheap preview
        // could be replayed against an expensive change.
        if (!QuoteMatchesStops(quote, effectiveStops))
        {
            return TripErrors.EditDeltaChanged;
        }

        var vehicleType = await context.VehicleTypes.FirstOrDefaultAsync(v => v.Id == quote.VehicleTypeId, ct);
        if (vehicleType is null)
        {
            return TripErrors.VehicleTypeNotFound;
        }

        if (newPassengerCount.HasValue)
        {
            if (newPassengerCount.Value < 1)
            {
                return TripErrors.InvalidPassengerCount;
            }

            // The quoted vehicle has to actually fit the party being applied.
            if (vehicleType.PassengerCapacity < newPassengerCount.Value)
            {
                return TripErrors.EditDeltaChanged;
            }
        }

        var stopResults = effectiveStops
            .Select((s, index) => TripStop.Create(
                new Domain.Trips.Coordinate(s.Latitude, s.Longitude), index, s.Label))
            .ToList();
        if (stopResults.Any(r => r.IsFailure))
        {
            return stopResults.First(r => r.IsFailure).Error;
        }

        // Same rule as the live re-quote: delta against the quote the trip currently points at.
        var currentQuote = await context.PricingQuotes.FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);
        var oldFinalFare = currentQuote?.FinalFare ?? 0m;
        var newVehicleTypeId = quote.VehicleTypeId == trip.VehicleTypeId ? (Guid?)null : quote.VehicleTypeId;

        return new TripRequoteResult(
            quote,
            stopResults.Select(r => r.Value).ToList(),
            newVehicleTypeId,
            oldFinalFare,
            quote.FinalFare,
            Math.Round(quote.FinalFare - oldFinalFare, 2),
            quote.CurrencyCode,
            quote.EncodedOverviewPolyline ?? string.Empty,
            quote.RouteSegmentsJson ?? string.Empty,
            (int)Math.Round(quote.TotalDistanceKm * 1000m),
            (int)Math.Round(quote.TotalDurationMin * 60m));
    }

    private static List<TripEditStop> EffectiveStops(Trip trip, IReadOnlyList<TripEditStop>? newStops) =>
        newStops is { Count: > 0 }
            ? [.. newStops]
            : [.. trip.Stops
                .OrderBy(s => s.Sequence)
                .Select(s => new TripEditStop(s.Coordinate.Latitude, s.Coordinate.Longitude, s.AddressLabel))];

    private const decimal CoordinateEpsilon = 0.000001m;

    private static bool QuoteMatchesStops(PricingQuote quote, IReadOnlyList<TripEditStop> stops)
    {
        var quoted = quote.Stops.ToList();
        if (quoted.Count != stops.Count)
        {
            return false;
        }

        for (var i = 0; i < stops.Count; i++)
        {
            if (Math.Abs(quoted[i].Latitude - stops[i].Latitude) > CoordinateEpsilon
                || Math.Abs(quoted[i].Longitude - stops[i].Longitude) > CoordinateEpsilon)
            {
                return false;
            }
        }

        return true;
    }
}
