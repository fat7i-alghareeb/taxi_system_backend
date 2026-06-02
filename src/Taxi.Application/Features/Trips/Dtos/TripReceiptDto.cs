namespace Taxi.Application.Features.Trips.Dtos;

public record TripReceiptDto(
    Guid TripId,
    string ReferenceCode,
    string Status,
    decimal GrossAmount,
    decimal NetAmount,
    decimal TaxAmount,
    string CurrencyCode,
    string PaymentMethod,
    string? PaymentReference,
    DateTimeOffset? PaidAtUtc,
    DateTimeOffset? CompletedAtUtc,
    decimal DistanceKm,
    decimal DurationMin,
    string VehicleTypeName,
    string? PassengerName,
    string IssuerName,
    bool InvoiceAvailable,
    string? InvoiceNumber,
    DateTimeOffset? InvoiceIssuedAtUtc,
    List<TripStopDto> Stops);
