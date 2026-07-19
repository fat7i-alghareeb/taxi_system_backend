using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Common;
using Taxi.Application.Features.Trips.Dtos;
using AppCoordinate = Taxi.Application.Common.Interfaces.Coordinate;

using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;
using Taxi.Domain.Pricing;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.UpdateTripStops;

public class UpdateTripStopsCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IDirectionsService directionsService,
    ITripAdminEditNotifier adminEditNotifier,
    TimeProvider timeProvider) : IRequestHandler<UpdateTripStopsCommand, Result<TripDto>>
{
    public async Task<Result<TripDto>> Handle(UpdateTripStopsCommand request, CancellationToken ct)
    {
        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (!currentUser.IsAdmin)
        {
            if (!Guid.TryParse(currentUser.Id, out var passengerId))
            {
                return TripErrors.PassengerNotFound;
            }

            if (trip.PassengerId != passengerId)
            {
                return TripErrors.NotOwnedByPassenger;
            }
        }

        if (request.Stops.Count < 2)
        {
            return TripErrors.InvalidStops;
        }

        var serviceCoords = request.Stops
            .Select(s => new AppCoordinate(s.Latitude, s.Longitude))
            .ToList();

        var directionResponse = await directionsService.GetDirectionsAsync(serviceCoords);

        var totalDistanceKm = Math.Round(directionResponse.TotalDistanceMeters / 1000m, 3);
        var totalDurationMin = Math.Round(directionResponse.TotalDurationSeconds / 60m, 2);

        var vehicleType = await context.VehicleTypes.FirstOrDefaultAsync(v => v.Id == trip.VehicleTypeId, ct);
        if (vehicleType is null)
        {
            return TripErrors.VehicleTypeNotFound;
        }

        var configs = await context.AppConfigs
            .Where(c => c.Key == AppConfigKeys.TripDiscountPercent || c.Key == AppConfigKeys.Currency)
            .ToListAsync(ct);

        var discountConfig = configs.FirstOrDefault(c => c.Key == AppConfigKeys.TripDiscountPercent);
        var discountPercent = decimal.TryParse(discountConfig?.Value, out var d) && d > 0 ? d : 0m;

        var currencyConfig = configs.FirstOrDefault(c => c.Key == AppConfigKeys.Currency);
        var currencyCode = string.IsNullOrWhiteSpace(currencyConfig?.Value) ? "EUR" : currencyConfig!.Value;

        var originalFare = PricingService.CalculateFare(vehicleType, (double)totalDistanceKm, (double)totalDurationMin);
        var finalFare = discountPercent > 0
            ? Math.Round(originalFare * (1 - (discountPercent / 100)), 2)
            : originalFare;

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

        var domainCoords = request.Stops
            .Select(s => new Domain.Trips.Coordinate(s.Latitude, s.Longitude))
            .ToList();

        var newQuoteResult = PricingQuote.Create(
            Guid.NewGuid(),
            trip.PassengerId,
            vehicleType.Id,
            totalDistanceKm,
            totalDurationMin,
            finalFare,
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

        var newQuote = newQuoteResult.Value;
        context.PricingQuotes.Add(newQuote);

        var stopResults = request.Stops
            .Select((s, index) => TripStop.Create(
                new Domain.Trips.Coordinate(s.Latitude, s.Longitude),
                index,
                s.Label))
            .ToList();

        if (stopResults.Any(r => r.IsFailure))
        {
            return stopResults.First(r => r.IsFailure).Error;
        }

        var newStops = stopResults.Select(r => r.Value).ToList();

        var updateResult = trip.UpdateStops(newStops);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        trip.UpdateQuote(newQuote.Id);
        await context.SaveChangesAsync(ct);

        // Update or create the TripRoute with recalculated route data.
        var existingRoute = await context.TripRoutes.FirstOrDefaultAsync(r => r.TripId == trip.Id, ct);
        if (existingRoute is not null)
        {
            context.TripRoutes.Remove(existingRoute);
        }

        var totalDistanceMeters = (int)Math.Round(totalDistanceKm * 1000m);
        var totalDurationSeconds = (int)Math.Round(totalDurationMin * 60m);
        var newRouteResult = TripRoute.Create(
            Guid.NewGuid(),
            trip.Id,
            directionResponse.OverviewPolyline,
            totalDistanceMeters,
            totalDurationSeconds,
            routeSegmentsJson);

        if (newRouteResult.IsSuccess)
        {
            context.TripRoutes.Add(newRouteResult.Value);
        }

        await context.SaveChangesAsync(ct);

        await adminEditNotifier.NotifyAsync(trip, TripEditKind.Route, ct);

        return await TripDtoBuilder.BuildAsync(context, trip, timeProvider.GetUtcNow(), ct);
    }
}
