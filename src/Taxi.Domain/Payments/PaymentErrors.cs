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

    public static readonly Error RefundUnavailable = Error.Validation(
        code: LocalizationKeys.Payment.RefundUnavailable,
        description: "This payment is not eligible for an automatic refund.");

    public static readonly Error RefundStripeDisabled = Error.Validation(
        code: LocalizationKeys.Payment.RefundStripeDisabled,
        description: "Stripe refunds are disabled for this environment.");

    public static readonly Error RefundFullyRefunded = Error.Conflict(
        code: LocalizationKeys.Payment.RefundFullyRefunded,
        description: "This payment has already been fully refunded.");

    public static Error RefundExceedsAvailable(decimal requestedAmount, decimal availableAmount) => Error.Validation(
        code: LocalizationKeys.Payment.RefundExceedsAvailable,
        description: $"Requested refund amount {requestedAmount:0.00} exceeds the available refundable balance {availableAmount:0.00}.",
        requestedAmount,
        availableAmount);

    public static readonly Error RefundDuplicate = Error.Conflict(
        code: LocalizationKeys.Payment.RefundDuplicate,
        description: "A matching refund is already pending or completed for this source.");

    public static readonly Error RefundForcedFailure = Error.Failure(
        code: LocalizationKeys.Payment.RefundForcedFailure,
        description: "Refund failure was forced by development configuration.");

    public static readonly Error RefundRetryBlocked = Error.Validation(
        code: LocalizationKeys.Payment.RefundRetryBlocked,
        description: "This refund is not eligible for retry.");
}

