namespace Taxi.Contracts.Responses.Payments;

public record StripePaymentResponse(
    string PaymentIntentId,
    string ClientSecret,
    string PublishableKey);
