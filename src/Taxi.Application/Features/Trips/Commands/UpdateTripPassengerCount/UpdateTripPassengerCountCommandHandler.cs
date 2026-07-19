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

namespace Taxi.Application.Features.Trips.Commands.UpdateTripPassengerCount;

public class UpdateTripPassengerCountCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IDirectionsService directionsService,
    ITripAdminEditNotifier adminEditNotifier,
    TimeProvider timeProvider) : IRequestHandler<UpdateTripPassengerCountCommand, Result<TripDto>>
{
    public async Task<Result<TripDto>> Handle(UpdateTripPassengerCountCommand request, CancellationToken ct)
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

        Guid? newVehicleTypeId = null;

        // When passenger count exceeds the current vehicle capacity, find the
        // smallest vehicle type that can accommodate the new count.
        var currentVehicleType = await context.VehicleTypes.FirstOrDefaultAsync(v => v.Id == trip.VehicleTypeId, ct);
        if (currentVehicleType is null)
        {
            return TripErrors.VehicleTypeNotFound;
        }

        var needsUpgrade = request.PassengerCount > currentVehicleType.PassengerCapacity;

        if (needsUpgrade)
        {
            var suitableVehicleType = await context.VehicleTypes
                .Where(v => v.IsActive && v.PassengerCapacity >= request.PassengerCount)
                .OrderBy(v => v.PassengerCapacity)
                .ThenBy(v => v.SortOrder)
                .FirstOrDefaultAsync(ct);

            if (suitableVehicleType is null)
            {
                return TripErrors.NoSuitableVehicleForPassengerCount;
            }

            newVehicleTypeId = suitableVehicleType.Id;

            // Recalculate fare for the upgraded vehicle type using existing stops.
            var stopCoords = trip.Stops
                .OrderBy(s => s.Sequence)
                .Select(s => new AppCoordinate(s.Coordinate.Latitude, s.Coordinate.Longitude))
                .ToList();

            var directionResponse = await directionsService.GetDirectionsAsync(stopCoords);
            var totalDistanceKm = Math.Round(directionResponse.TotalDistanceMeters / 1000m, 3);
            var totalDurationMin = Math.Round(directionResponse.TotalDurationSeconds / 60m, 2);

            var configs = await context.AppConfigs
                .Where(c => c.Key == AppConfigKeys.TripDiscountPercent || c.Key == AppConfigKeys.Currency)
                .ToListAsync(ct);

            var discountConfig = configs.FirstOrDefault(c => c.Key == AppConfigKeys.TripDiscountPercent);
            var discountPercent = decimal.TryParse(discountConfig?.Value, out var d) && d > 0 ? d : 0m;

            var currencyConfig = configs.FirstOrDefault(c => c.Key == AppConfigKeys.Currency);
            var currencyCode = string.IsNullOrWhiteSpace(currencyConfig?.Value) ? "EUR" : currencyConfig!.Value;

            var originalFare = PricingService.CalculateFare(suitableVehicleType, (double)totalDistanceKm, (double)totalDurationMin);
            var finalFare = discountPercent > 0
                ? Math.Round(originalFare * (1 - (discountPercent / 100)), 2)
                : originalFare;

            var domainCoords = trip.Stops
                .OrderBy(s => s.Sequence)
                .Select(s => new Domain.Trips.Coordinate(s.Coordinate.Latitude, s.Coordinate.Longitude))
                .ToList();

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

            var newQuoteResult = PricingQuote.Create(
                Guid.NewGuid(),
                trip.PassengerId,
                suitableVehicleType.Id,
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

            context.PricingQuotes.Add(newQuoteResult.Value);
            trip.UpdateQuote(newQuoteResult.Value.Id, newVehicleTypeId);
        }

        var updateResult = trip.UpdatePassengerCount(request.PassengerCount, newVehicleTypeId);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        await adminEditNotifier.NotifyAsync(trip, TripEditKind.Passengers, ct);

        return await TripDtoBuilder.BuildAsync(context, trip, timeProvider.GetUtcNow(), ct);
    }
}
