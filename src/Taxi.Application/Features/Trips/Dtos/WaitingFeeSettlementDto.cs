namespace Taxi.Application.Features.Trips.Dtos;

public record WaitingFeeSettlementDto(
    decimal Amount,
    string CurrencyCode,
    StripePaymentDto? StripePayment);
