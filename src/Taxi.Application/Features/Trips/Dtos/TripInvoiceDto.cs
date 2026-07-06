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
    decimal FareAmount,
    decimal WaitingFeeAmount,
    decimal DiscountAmount,
    decimal TotalPaidAmount,
    decimal RefundedAmount,
    decimal RemainingAmount,
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
    string? PassengerEmail,
    List<TripInvoiceStopDto> Stops);

public record TripInvoiceStopDto(
    int Sequence,
    string? Label,
    DateTimeOffset? CompletedAtUtc);
