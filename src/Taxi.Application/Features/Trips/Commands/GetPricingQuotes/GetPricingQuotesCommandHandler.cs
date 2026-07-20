using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Application.Features.Wallet.Common;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;
using Taxi.Domain.Pricing;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.GetPricingQuotes;

public class GetPricingQuotesCommandHandler(
    IAppDbContext context,
    IDirectionsService directionsService,
    IUser currentUser,
    ILanguageContext languageContext,
    IWalletDebtGuard debtGuard) : IRequestHandler<GetPricingQuotesCommand, Result<PricingQuotesListDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<PricingQuotesListDto>> Handle(GetPricingQuotesCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var passengerId))
        {
            return TripErrors.PassengerNotFound;
        }

        // Refuse a customer in debt before spending a Directions call and persisting quote rows.
        if (await debtGuard.CheckAsync(passengerId, ct) is { } debtError)
        {
            return debtError;
        }

        var stops = request.Stops;
        var serviceCoordinates = stops.Select(s => new Application.Common.Interfaces.Coordinate(s.Latitude, s.Longitude)).ToList();

        var directionResponse = await directionsService.GetDirectionsAsync(serviceCoordinates);

        var totalDistanceKm = Math.Round(directionResponse.TotalDistanceMeters / 1000m, 3);
        var totalDurationMin = Math.Round(directionResponse.TotalDurationSeconds / 60m, 2);

        var vehicleTypes = await _context.VehicleTypes
            .Where(vt => vt.IsActive)
            .OrderBy(vt => vt.SortOrder)
            .ToListAsync(ct);

        var configs = await _context.AppConfigs
            .Where(c => c.Key == AppConfigKeys.TripDiscountPercent || c.Key == AppConfigKeys.Currency)
            .ToListAsync(ct);

        var discountConfig = configs.FirstOrDefault(c => c.Key == AppConfigKeys.TripDiscountPercent);
        var discountPercent = decimal.TryParse(discountConfig?.Value, out var d) && d > 0 ? d : 0m;

        var currencyConfig = configs.FirstOrDefault(c => c.Key == AppConfigKeys.Currency);
        var currencyCode = string.IsNullOrWhiteSpace(currencyConfig?.Value) ? "EUR" : currencyConfig!.Value;

        var validUntil = DateTime.UtcNow.AddMinutes(15);
        var quotes = new List<PricingQuote>();
        var domainCoordinates = stops.Select(s => new Domain.Trips.Coordinate(s.Latitude, s.Longitude)).ToList();

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

        foreach (var vehicleType in vehicleTypes)
        {
            var originalFare = PricingService.CalculateFare(
                vehicleType,
                (double)totalDistanceKm,
                (double)totalDurationMin);

            var finalFare = discountPercent > 0
                ? Math.Round(originalFare * (1 - (discountPercent / 100)), 2)
                : originalFare;

            var quoteResult = PricingQuote.Create(
                Guid.NewGuid(),
                passengerId,
                vehicleType.Id,
                totalDistanceKm,
                totalDurationMin,
                finalFare,
                originalFare,
                discountPercent,
                currencyCode,
                validUntil,
                domainCoordinates,
                directionResponse.OverviewPolyline,
                routeSegmentsJson);

            if (quoteResult.IsFailure)
            {
                return quoteResult.Error;
            }

            quotes.Add(quoteResult.Value);
            _context.PricingQuotes.Add(quoteResult.Value);
        }

        await _context.SaveChangesAsync(ct);

        var lang = languageContext.Language;

        var quoteDtos = quotes.Zip(vehicleTypes, (quote, vt) =>
            new PricingQuoteDto(
                quote.Id,
                vt.Id,
                lang == Languages.Ar ? vt.Name.Ar : lang == Languages.Nl ? vt.Name.Nl : vt.Name.En,
                vt.PassengerCapacity,
                quote.OriginalFare,
                quote.FinalFare,
                quote.DiscountPercent,
                quote.CurrencyCode,
                quote.ValidUntil)).ToList();

        return new PricingQuotesListDto(totalDistanceKm, totalDurationMin, quoteDtos);
    }
}

