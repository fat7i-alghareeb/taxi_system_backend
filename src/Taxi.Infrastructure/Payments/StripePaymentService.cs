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
        CancellationToken ct = default)
    {
        try
        {
            var service = new PaymentIntentService();
            var options = new PaymentIntentCreateOptions
            {
                Amount = ToMinorUnits(amount),
                Currency = currency.ToLowerInvariant(),
                CaptureMethod = "automatic",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                Metadata = new Dictionary<string, string>
                {
                    ["tripId"] = tripId.ToString(),
                    ["passengerId"] = passengerId.ToString(),
                    ["quoteId"] = quoteId.ToString(),
                },
            };

            var requestOptions = new RequestOptions { IdempotencyKey = quoteId.ToString() };

            var intent = await service.CreateAsync(options, requestOptions, ct);
            return new StripePaymentIntentResult(intent.Id, intent.ClientSecret, this.settings.PublishableKey);
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

    public async Task<Result<StripeRefundResult>> CreateRefundAsync(string paymentIntentId, CancellationToken ct = default)
    {
        try
        {
            var service = new RefundService();
            var refund = await service.CreateAsync(
                new RefundCreateOptions { PaymentIntent = paymentIntentId },
                cancellationToken: ct);
            var amount = FromMinorUnits(refund.Amount);
            return new StripeRefundResult(refund.Id, amount, (refund.Currency ?? string.Empty).ToUpperInvariant());
        }
        catch (StripeException ex)
        {
            this.logger.LogError(ex, "Stripe refund failed for {PaymentIntentId}", paymentIntentId);
            return PaymentErrors.StripeInitiationFailed;
        }
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
