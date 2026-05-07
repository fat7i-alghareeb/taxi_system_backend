using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.GetPricingQuotes;

public class GetPricingQuotesCommandHandler(
    IAppDbContext context,
    IDirectionsService directionsService,
    IPricingService pricingService,
    IUser currentUser,
    ILanguageContext languageContext) : IRequestHandler<GetPricingQuotesCommand, Result<List<PricingQuoteDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<PricingQuoteDto>>> Handle(GetPricingQuotesCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var passengerId))
        {
            return TripErrors.PassengerNotFound;
        }

        var totalDistanceMeters = 0;
        var totalDurationSeconds = 0;
        var stops = request.Stops;

        for (var i = 0; i < stops.Count - 1; i++)
        {
            var origin = stops[i];
            var destination = stops[i + 1];

            var leg = await directionsService.GetDirectionsAsync(
                origin.Latitude, origin.Longitude,
                destination.Latitude, destination.Longitude);

            totalDistanceMeters += leg.DistanceMeters;
            totalDurationSeconds += leg.DurationSeconds;
        }

        var totalDistanceKm = Math.Round(totalDistanceMeters / 1000m, 3);
        var totalDurationMin = Math.Round(totalDurationSeconds / 60m, 2);

        var vehicleTypes = await _context.VehicleTypes
            .Where(vt => vt.IsActive)
            .OrderBy(vt => vt.SortOrder)
            .ToListAsync(ct);

        var coordinates = stops.Select(s => new Coordinate(s.Latitude, s.Longitude)).ToList();
        var validUntil = DateTime.UtcNow.AddMinutes(15);
        var quotes = new List<PricingQuote>();

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
                coordinates);

            if (quoteResult.IsFailure)
            {
                return quoteResult.Error;
            }

            quotes.Add(quoteResult.Value);
            _context.PricingQuotes.Add(quoteResult.Value);
        }

        await _context.SaveChangesAsync(ct);

        var lang = languageContext.Language;

        var result = quotes.Zip(vehicleTypes, (quote, vt) => new PricingQuoteDto(
            quote.Id,
            vt.Id,
            lang == Languages.Ar ? vt.Name.Ar : lang == Languages.Nl ? vt.Name.Nl : vt.Name.En,
            quote.TotalDistanceKm,
            quote.TotalDurationMin,
            quote.FinalFare,
            quote.CurrencyCode,
            quote.ValidUntil)).ToList();

        return result;
    }
}
