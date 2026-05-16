using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Stripe;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Infrastructure.Settings;

namespace Taxi.Infrastructure.Payments;

public sealed class StripeWebhookValidator : IStripeWebhookValidator
{
    private readonly StripeSettings settings;
    private readonly ILogger<StripeWebhookValidator> logger;

    public StripeWebhookValidator(IOptions<AppSettings> appSettings, ILogger<StripeWebhookValidator> logger)
    {
        this.settings = appSettings.Value.Stripe;
        this.logger = logger;
    }

    public Result<StripeWebhookEvent> Parse(string json, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(this.settings.WebhookSecret))
        {
            this.logger.LogError("Stripe webhook secret is not configured.");
            return PaymentErrors.StripeSignatureInvalid;
        }

        try
        {
            var evt = EventUtility.ConstructEvent(
                json,
                signatureHeader,
                this.settings.WebhookSecret,
                throwOnApiVersionMismatch: false);

            return MapEvent(evt);
        }
        catch (StripeException ex)
        {
            this.logger.LogWarning(ex, "Stripe webhook signature validation failed");
            return PaymentErrors.StripeSignatureInvalid;
        }
    }

    private static StripeWebhookEvent MapEvent(Event evt)
    {
        switch (evt.Type)
        {
            case "payment_intent.succeeded":
            {
                var pi = (PaymentIntent)evt.Data.Object;
                var amount = pi.AmountReceived / 100m;
                return new StripeWebhookEvent(
                    EventId: evt.Id,
                    Kind: StripeWebhookEventKind.PaymentIntentSucceeded,
                    PaymentIntentId: pi.Id,
                    ChargeId: pi.LatestChargeId,
                    AmountReceived: amount,
                    Currency: pi.Currency?.ToUpperInvariant(),
                    FailureCode: null,
                    FailureMessage: null,
                    RefundedAmount: null);
            }

            case "payment_intent.payment_failed":
            {
                var pi = (PaymentIntent)evt.Data.Object;
                return new StripeWebhookEvent(
                    EventId: evt.Id,
                    Kind: StripeWebhookEventKind.PaymentIntentFailed,
                    PaymentIntentId: pi.Id,
                    ChargeId: null,
                    AmountReceived: null,
                    Currency: pi.Currency?.ToUpperInvariant(),
                    FailureCode: pi.LastPaymentError?.Code,
                    FailureMessage: pi.LastPaymentError?.Message,
                    RefundedAmount: null);
            }

            case "payment_intent.canceled":
            {
                var pi = (PaymentIntent)evt.Data.Object;
                return new StripeWebhookEvent(
                    EventId: evt.Id,
                    Kind: StripeWebhookEventKind.PaymentIntentCanceled,
                    PaymentIntentId: pi.Id,
                    ChargeId: null,
                    AmountReceived: null,
                    Currency: pi.Currency?.ToUpperInvariant(),
                    FailureCode: pi.CancellationReason,
                    FailureMessage: null,
                    RefundedAmount: null);
            }

            case "charge.refunded":
            {
                var charge = (Charge)evt.Data.Object;
                var amount = charge.AmountRefunded / 100m;
                return new StripeWebhookEvent(
                    EventId: evt.Id,
                    Kind: StripeWebhookEventKind.ChargeRefunded,
                    PaymentIntentId: charge.PaymentIntentId,
                    ChargeId: charge.Id,
                    AmountReceived: null,
                    Currency: charge.Currency?.ToUpperInvariant(),
                    FailureCode: null,
                    FailureMessage: null,
                    RefundedAmount: amount);
            }

            default:
                return new StripeWebhookEvent(
                    EventId: evt.Id,
                    Kind: StripeWebhookEventKind.Unhandled,
                    PaymentIntentId: null,
                    ChargeId: null,
                    AmountReceived: null,
                    Currency: null,
                    FailureCode: null,
                    FailureMessage: null,
                    RefundedAmount: null);
        }
    }
}
