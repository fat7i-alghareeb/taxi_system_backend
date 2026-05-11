namespace Taxi.Application.Features.Trips.Dtos;

public record PricingQuoteDto(
    Guid QuoteId,
    Guid VehicleTypeId,
    string VehicleTypeName,
    int Capacity,
    decimal OriginalFare,
    decimal FinalFare,
    decimal DiscountPercent,
    string CurrencyCode,
    DateTime ValidUntil);

