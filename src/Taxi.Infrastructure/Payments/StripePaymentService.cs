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
