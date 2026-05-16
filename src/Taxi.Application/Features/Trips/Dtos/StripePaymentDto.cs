namespace Taxi.Application.Features.Trips.Dtos;

public record StripePaymentDto(
    string PaymentIntentId,
    string ClientSecret,
    string PublishableKey);
