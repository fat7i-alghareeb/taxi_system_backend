namespace Taxi.Application.Features.PaymentMethods.Dtos;

/// <summary>
/// Details the app needs to open the Stripe PaymentSheet in "setup" mode so the customer can
/// save a reusable payment method. Consent to future ride-related charges is captured in the app.
/// </summary>
public record PaymentMethodSetupDto(
    string SetupIntentId,
    string ClientSecret,
    string PublishableKey,
    string CustomerId,
    string EphemeralKeySecret);
