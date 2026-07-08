namespace Taxi.Application.Features.Trips.Dtos;

/// <summary>
/// A trip's money breakdown: what was charged (fare + fees), how it was paid
/// (wallet vs card), what is still unpaid, and what was refunded. Used by the
/// customer receipt and the admin trip-financials view.
/// </summary>
public record TripFinancialsDto(
    string CurrencyCode,
    decimal FareAmount,
    decimal WaitingFeeAmount,
    decimal TotalCharged,
    decimal WalletPaidAmount,
    decimal CardPaidAmount,
    decimal TotalPaidAmount,
    decimal UnpaidAmount,
    decimal RefundedAmount,
    IReadOnlyList<TripPaymentLineDto> Payments,
    IReadOnlyList<TripRefundLineDto> Refunds);

public record TripPaymentLineDto(
    string Kind,
    string Method,
    decimal Amount,
    string Status,
    DateTimeOffset? ProcessedAtUtc,
    string? StripePaymentMethodType);

public record TripRefundLineDto(
    decimal Amount,
    string Status,
    string SourceType,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? CompletedAtUtc);
