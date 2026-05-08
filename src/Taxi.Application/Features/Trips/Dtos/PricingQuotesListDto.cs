namespace Taxi.Application.Features.Trips.Dtos;

public record PricingQuotesListDto(
    decimal TotalDistanceKm,
    decimal TotalDurationMin,
    List<PricingQuoteDto> Quotes);

