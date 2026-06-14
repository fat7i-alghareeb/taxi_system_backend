using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Stripe;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Infrastructure.Settings;

namespace Taxi.Infrastructure.Payments;

public sealed class StripePaymentService : IStripePaymentService
{
    private readonly StripeSettings settings;
    private readonly ILogger<StripePaymentService> logger;

    public StripePaymentService(IOptions<AppSettings> appSettings, ILogger<StripePaymentService> logger)
    {
        this.settings = appSettings.Value.Stripe;
        this.logger = logger;

        if (!string.IsNullOrWhiteSpace(this.settings.SecretKey))
        {
            StripeConfiguration.ApiKey = this.settings.SecretKey;
        }
    }

    public async Task<string?> GetChargePaymentMethodTypeAsync(string chargeId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(chargeId))
        {
            return null;
        }

        try
        {
            var chargeService = new ChargeService();
            var charge = await chargeService.GetAsync(chargeId, cancellationToken: ct);
            return charge.PaymentMethodDetails?.Type;
        }
        catch (StripeException ex)
        {
            // Best-effort enrichment for the invoice — never fail the webhook over this.
            this.logger.LogWarning(ex, "Could not resolve payment-method type for charge {ChargeId}", chargeId);
            return null;
        }
    }

    public async Task<Result<StripePaymentIntentResult>> CreatePaymentIntentAsync(
        Guid quoteId,
        decimal amount,
        string currency,
        Guid tripId,
        Guid passengerId,
        string? existingStripeCustomerId,
        string? passengerEmail,
        string? passengerPhone,
        string passengerName,
        string? passengerPreferredLanguage,
        CancellationToken ct = default)
    {
        try
        {
            var customerService = new CustomerService();
            string customerId;

            if (!string.IsNullOrWhiteSpace(existingStripeCustomerId))
            {
                customerId = existingStripeCustomerId;
            }
            else
            {
                var customer = await customerService.CreateAsync(
                    new CustomerCreateOptions
                    {
                        Name = passengerName,
                        Email = passengerEmail,
                        Phone = passengerPhone,
                        Metadata = new Dictionary<string, string>
                        {
                            ["passengerId"] = passengerId.ToString(),
                        },
                    },
                    cancellationToken: ct);
                customerId = customer.Id;
            }

            var ekService = new EphemeralKeyService();
            var ephemeralKey = await ekService.CreateAsync(
                new EphemeralKeyCreateOptions { Customer = customerId },
                cancellationToken: ct);

            var intentService = new PaymentIntentService();
            var options = new PaymentIntentCreateOptions
            {
                Amount = ToMinorUnits(amount),
                Currency = currency.ToLowerInvariant(),
                CaptureMethod = "automatic",
                Customer = customerId,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                PaymentMethodOptions = new PaymentIntentPaymentMethodOptionsOptions
                {
                    // Save the card off-session so a post-trip waiting-fee surcharge can be
                    // charged later without the customer present. Scoped to cards only, so
                    // one-off methods (Klarna/iDEAL) remain available for the upfront payment.
                    Card = new PaymentIntentPaymentMethodOptionsCardOptions
                    {
                        SetupFutureUsage = "off_session",
                    },
                    Klarna = new PaymentIntentPaymentMethodOptionsKlarnaOptions
                    {
                        PreferredLocale = ResolveKlarnaLocale(passengerPreferredLanguage),
                    },
                },
                Metadata = new Dictionary<string, string>
                {
                    ["tripId"] = tripId.ToString(),
                    ["passengerId"] = passengerId.ToString(),
                    ["quoteId"] = quoteId.ToString(),
                },
            };

            var requestOptions = new RequestOptions { IdempotencyKey = quoteId.ToString() };

            var intent = await intentService.CreateAsync(options, requestOptions, ct);
            return new StripePaymentIntentResult(
                intent.Id,
                intent.ClientSecret,
                this.settings.PublishableKey,
                customerId,
                ephemeralKey.Secret);
        }
        catch (StripeException ex)
        {
            this.logger.LogError(ex, "Stripe PaymentIntent creation failed for quote {QuoteId}", quoteId);
            return PaymentErrors.StripeInitiationFailed;
        }
    }

    public async Task<Result<StripePaymentIntentResult>> CreateWaitingFeePaymentIntentAsync(
        decimal amount,
        string currency,
        Guid tripId,
        Guid passengerId,
        string? existingStripeCustomerId,
        string? passengerEmail,
        string? passengerPhone,
        string passengerName,
        CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            return PaymentErrors.InvalidAmount;
        }

        try
        {
            var customerService = new CustomerService();
            string customerId;

            if (!string.IsNullOrWhiteSpace(existingStripeCustomerId))
            {
                customerId = existingStripeCustomerId;
            }
            else
            {
                var customer = await customerService.CreateAsync(
                    new CustomerCreateOptions
                    {
                        Name = passengerName,
                        Email = passengerEmail,
                        Phone = passengerPhone,
                        Metadata = new Dictionary<string, string>
                        {
                            ["passengerId"] = passengerId.ToString(),
                        },
                    },
                    cancellationToken: ct);
                customerId = customer.Id;
            }

            var ekService = new EphemeralKeyService();
            var ephemeralKey = await ekService.CreateAsync(
                new EphemeralKeyCreateOptions { Customer = customerId },
                cancellationToken: ct);

            var intentService = new PaymentIntentService();
            var options = new PaymentIntentCreateOptions
            {
                Amount = ToMinorUnits(amount),
                Currency = currency.ToLowerInvariant(),
                CaptureMethod = "automatic",
                Customer = customerId,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                Metadata = new Dictionary<string, string>
                {
                    ["tripId"] = tripId.ToString(),
                    ["passengerId"] = passengerId.ToString(),
                    ["kind"] = "waiting_fee",
                },
            };

            // Idempotent per trip: repeated settle attempts return the same intent
            // (and client secret) instead of creating duplicate charges.
            var requestOptions = new RequestOptions { IdempotencyKey = $"waiting-fee-settle-{tripId}" };

            var intent = await intentService.CreateAsync(options, requestOptions, ct);
            return new StripePaymentIntentResult(
                intent.Id,
                intent.ClientSecret,
                this.settings.PublishableKey,
                customerId,
                ephemeralKey.Secret);
        }
        catch (StripeException ex)
        {
            this.logger.LogError(ex, "Stripe waiting-fee PaymentIntent creation failed for trip {TripId}", tripId);
            return PaymentErrors.StripeInitiationFailed;
        }
    }

    public async Task<Result<Success>> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default)
    {
        try
        {
            var service = new PaymentIntentService();
            await service.CancelAsync(paymentIntentId, cancellationToken: ct);
            return Result.Success;
        }
        catch (StripeException ex)
        {
            this.logger.LogWarning(ex, "Stripe PaymentIntent cancel failed for {PaymentIntentId}", paymentIntentId);
            return PaymentErrors.StripeInitiationFailed;
        }
    }

    public async Task<Result<StripeSurchargeResult>> ChargeWaitingFeeAsync(
        string originalPaymentIntentId,
        decimal amount,
        string currency,
        Guid tripId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            return PaymentErrors.InvalidAmount;
        }

        try
        {
            var intentService = new PaymentIntentService();

            // The original (on-session) intent has the saved customer + payment method.
            var original = await intentService.GetAsync(originalPaymentIntentId, cancellationToken: ct);
            if (string.IsNullOrWhiteSpace(original.CustomerId) || string.IsNullOrWhiteSpace(original.PaymentMethodId))
            {
                this.logger.LogWarning(
                    "Cannot charge waiting fee for trip {TripId}: original intent {IntentId} has no reusable customer/payment method (likely a one-off method).",
                    tripId, originalPaymentIntentId);
                return PaymentErrors.StripeInitiationFailed;
            }

            var options = new PaymentIntentCreateOptions
            {
                Amount = ToMinorUnits(amount),
                Currency = currency.ToLowerInvariant(),
                Customer = original.CustomerId,
                PaymentMethod = original.PaymentMethodId,
                Confirm = true,
                OffSession = true,
                CaptureMethod = "automatic",
                Metadata = new Dictionary<string, string>
                {
                    ["tripId"] = tripId.ToString(),
                    ["kind"] = "waiting_fee",
                },
            };

            var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
            var intent = await intentService.CreateAsync(options, requestOptions, ct);

            return new StripeSurchargeResult(
                intent.Id,
                intent.Status,
                intent.LatestChargeId,
                string.Equals(intent.Status, "requires_action", StringComparison.OrdinalIgnoreCase));
        }
        catch (StripeException ex)
        {
            // Off-session declines and authentication_required (SCA) surface here with
            // the failed PaymentIntent attached. Report them as a soft result so the
            // caller can record the outcome and prompt on-session settlement.
            var pi = ex.StripeError?.PaymentIntent;
            var requiresAction = string.Equals(ex.StripeError?.Code, "authentication_required", StringComparison.OrdinalIgnoreCase);
            this.logger.LogWarning(
                ex, "Off-session waiting-fee charge failed for trip {TripId}: code={Code}", tripId, ex.StripeError?.Code);

            if (pi is not null)
            {
                return new StripeSurchargeResult(pi.Id, pi.Status ?? "failed", pi.LatestChargeId, requiresAction);
            }

            return PaymentErrors.StripeInitiationFailed;
        }
    }

    public async Task<Result<StripeRefundResult>> CreateRefundAsync(string paymentIntentId, decimal? refundAmount = null, CancellationToken ct = default)
    {
        try
        {
            var service = new RefundService();
            var refund = await service.CreateAsync(
                new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId,
                    Amount = refundAmount.HasValue ? ToMinorUnits(refundAmount.Value) : null,
                },
                cancellationToken: ct);
            var refundedAmount = FromMinorUnits(refund.Amount);
            return new StripeRefundResult(refund.Id, refundedAmount, (refund.Currency ?? string.Empty).ToUpperInvariant());
        }
        catch (StripeException ex)
        {
            this.logger.LogError(ex, "Stripe refund failed for {PaymentIntentId}", paymentIntentId);
            return PaymentErrors.StripeInitiationFailed;
        }
    }

    private static string ResolveKlarnaLocale(string? preferredLanguage)
    {
        var lang = (preferredLanguage ?? "en").Trim().ToLowerInvariant();
        return lang switch
        {
            "nl" => "nl-NL",
            "de" => "de-DE",
            "fr" => "fr-FR",
            "es" => "es-ES",
            "pl" => "pl-PL",
            "ro" => "ro-RO",
            "uk" => "uk-UA",
            "ar" => "en-NL",
            _ => "en-NL",
        };
    }

    private static long ToMinorUnits(decimal amount)
    {
        // EUR / USD style 2-decimal currencies. Zero-decimal currencies (JPY, KRW) are not in scope.
        return (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
    }

    private static decimal FromMinorUnits(long minor)
    {
        return minor / 100m;
    }
}
