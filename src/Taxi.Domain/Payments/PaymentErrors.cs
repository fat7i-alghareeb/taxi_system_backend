using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Payments;

public static class PaymentErrors
{
    public static readonly Error InvalidAmount = Error.Validation(
        code: LocalizationKeys.Payment.InvalidAmount,
        description: "Amount must be greater than zero.");

    public static readonly Error NotFound = Error.NotFound(
        code: LocalizationKeys.Payment.NotFound,
        description: "Payment not found.");

    public static readonly Error StripeInitiationFailed = Error.Failure(
        code: LocalizationKeys.Payment.StripeInitiationFailed,
        description: "Failed to initiate payment with Stripe.");

    public static readonly Error StripeSignatureInvalid = Error.Validation(
        code: LocalizationKeys.Payment.StripeSignatureInvalid,
        description: "Stripe webhook signature is invalid.");

    public static readonly Error StripeIntentNotFound = Error.NotFound(
        code: LocalizationKeys.Payment.StripeIntentNotFound,
        description: "No payment record matches the Stripe payment intent.");

    public static readonly Error WebhookHandlingFailed = Error.Failure(
        code: LocalizationKeys.Payment.WebhookHandlingFailed,
        description: "Failed to process Stripe webhook event.");
}

