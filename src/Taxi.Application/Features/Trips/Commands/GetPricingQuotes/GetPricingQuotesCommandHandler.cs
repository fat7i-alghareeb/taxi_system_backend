using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.GetPricingQuotes;

public class GetPricingQuotesCommandHandler(
    IAppDbContext context,
    IDirectionsService directionsService,
    IPricingService pricingService,
    IUser currentUser,
    ILanguageContext languageContext) : IRequestHandler<GetPricingQuotesCommand, Result<PricingQuotesListDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<PricingQuotesListDto>> Handle(GetPricingQuotesCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var passengerId))
        {
            return TripErrors.PassengerNotFound;
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

        var validUntil = DateTime.UtcNow.AddMinutes(15);
        var quotes = new List<PricingQuote>();
        var domainCoordinates = stops.Select(s => new Domain.Trips.Coordinate(s.Latitude, s.Longitude)).ToList();

        foreach (var vehicleType in vehicleTypes)
        {
            var fare = pricingService.CalculateFare(
                vehicleType,
                (double)totalDistanceKm,
                (double)totalDurationMin);

            var quoteResult = PricingQuote.Create(
                Guid.NewGuid(),
                passengerId,
                vehicleType.Id,
                totalDistanceKm,
                totalDurationMin,
                fare,
                vehicleType.CurrencyCode,
                validUntil,
                domainCoordinates);

            if (quoteResult.IsFailure)
            {
                return quoteResult.Error;
            }

            quotes.Add(quoteResult.Value);
            _context.PricingQuotes.Add(quoteResult.Value);
        }

        await _context.SaveChangesAsync(ct);

        var discountConfig = await _context.AppConfigs
            .FirstOrDefaultAsync(c => c.Key == AppConfigKeys.TripDiscountPercent, ct);
        var discountPercent = decimal.TryParse(discountConfig?.Value, out var d) && d > 0 ? d : 0m;

        var lang = languageContext.Language;

        var quoteDtos = quotes.Zip(vehicleTypes, (quote, vt) =>
        {
            var originalFare = quote.FinalFare;
            var finalFare = discountPercent > 0
                ? Math.Round(originalFare * (1 - (discountPercent / 100)), 2)
                : originalFare;

            return new PricingQuoteDto(
                quote.Id,
                vt.Id,
                lang == Languages.Ar ? vt.Name.Ar : lang == Languages.Nl ? vt.Name.Nl : vt.Name.En,
                vt.PassengerCapacity,
                originalFare,
                finalFare,
                discountPercent,
                quote.CurrencyCode,
                quote.ValidUntil);
        }).ToList();

        return new PricingQuotesListDto(totalDistanceKm, totalDurationMin, quoteDtos);
    }
}

