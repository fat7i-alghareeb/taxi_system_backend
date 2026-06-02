namespace Taxi.Application.Features.Trips.Dtos;

public record TripInvoiceDto(
    Guid InvoiceId,
    Guid TripId,
    string InvoiceNumber,
    DateTimeOffset IssuedAtUtc,
    string CurrencyCode,
    decimal GrossAmount,
    decimal NetAmount,
    decimal TaxRate,
    decimal TaxAmount,
    string PaymentMethod,
    string? PaymentReference,
    DateTimeOffset? PaidAtUtc,
    string IssuerName,
    string IssuerAddress,
    string? IssuerVatNumber,
    string TripReferenceCode,
    DateTimeOffset? TripCompletedAtUtc,
    decimal DistanceKm,
    decimal DurationMin,
    string VehicleTypeName,
    string? PassengerName,
    List<TripInvoiceStopDto> Stops);

public record TripInvoiceStopDto(
    int Sequence,
    string? Label,
    DateTimeOffset? CompletedAtUtc);
