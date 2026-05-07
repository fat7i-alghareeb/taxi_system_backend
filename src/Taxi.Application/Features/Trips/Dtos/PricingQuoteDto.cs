namespace Taxi.Application.Features.Trips.Dtos;

public record PricingQuoteDto(
    Guid QuoteId,
    Guid VehicleTypeId,
    string VehicleTypeName,
    decimal TotalDistanceKm,
    decimal TotalDurationMin,
    decimal FinalFare,
    string CurrencyCode,
    DateTime ValidUntil);
