using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Stripe;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.PaymentMethods;
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

    public async Task<Result<StripePaymentIntentResult>> CreateFareAdjustmentPaymentIntentAsync(
        decimal amount,
        string currency,
        Guid tripId,
        Guid passengerId,
        Guid pendingEditId,
        string idempotencyKey,
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
                // Keep the card reusable for later off-session ride-related charges.
                SetupFutureUsage = "off_session",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                Metadata = new Dictionary<string, string>
                {
                    ["tripId"] = tripId.ToString(),
                    ["passengerId"] = passengerId.ToString(),
                    ["kind"] = "fare_adjustment",
                    ["pendingEditId"] = pendingEditId.ToString(),
                },
            };

            // Idempotent per edit: a retried apply reuses the same intent/client secret.
            var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };

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
            this.logger.LogError(ex, "Stripe fare-adjustment PaymentIntent creation failed for trip {TripId}", tripId);
            return PaymentErrors.StripeInitiationFailed;
        }
    }

    public async Task<Result<StripePaymentIntentResult>> CreateTopUpPaymentIntentAsync(
        Guid walletTransactionId,
        decimal amount,
        string currency,
        Guid userId,
        string? existingStripeCustomerId,
        string? userEmail,
        string? userPhone,
        string userName,
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
                        Name = userName,
                        Email = userEmail,
                        Phone = userPhone,
                        Metadata = new Dictionary<string, string>
                        {
                            ["passengerId"] = userId.ToString(),
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
                    // Resolved by the payment_intent.succeeded webhook to credit the wallet ledger.
                    ["type"] = "wallet_topup",
                    ["userId"] = userId.ToString(),
                    ["walletTransactionId"] = walletTransactionId.ToString(),
                },
            };

            // Idempotent per top-up: a retried request reuses the same intent/client secret.
            var requestOptions = new RequestOptions { IdempotencyKey = $"wallet-topup-{walletTransactionId}" };

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
            this.logger.LogError(ex, "Stripe wallet top-up PaymentIntent creation failed for user {UserId}", userId);
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

    public async Task<Result<StripeSetupIntentResult>> CreateSetupIntentAsync(
        Guid userId,
        string? existingStripeCustomerId,
        string? userEmail,
        string? userPhone,
        string userName,
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
                        Name = userName,
                        Email = userEmail,
                        Phone = userPhone,
                        Metadata = new Dictionary<string, string>
                        {
                            ["passengerId"] = userId.ToString(),
                        },
                    },
                    cancellationToken: ct);
                customerId = customer.Id;
            }

            var ekService = new EphemeralKeyService();
            var ephemeralKey = await ekService.CreateAsync(
                new EphemeralKeyCreateOptions { Customer = customerId },
                cancellationToken: ct);

            var setupService = new SetupIntentService();
            var setupIntent = await setupService.CreateAsync(
                new SetupIntentCreateOptions
                {
                    Customer = customerId,
                    // Save the method for later customer-not-present ride-related charges.
                    Usage = "off_session",
                    AutomaticPaymentMethods = new SetupIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                        AllowRedirects = "never",
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["passengerId"] = userId.ToString(),
                        ["purpose"] = "save_reusable_method",
                    },
                },
                cancellationToken: ct);

            return new StripeSetupIntentResult(
                setupIntent.Id,
                setupIntent.ClientSecret,
                this.settings.PublishableKey,
                customerId,
                ephemeralKey.Secret);
        }
        catch (StripeException ex)
        {
            this.logger.LogError(ex, "Stripe SetupIntent creation failed for user {UserId}", userId);
            return PaymentErrors.StripeInitiationFailed;
        }
    }

    public async Task<Result<StripePaymentMethodDetails>> GetPaymentMethodDetailsAsync(
        string paymentMethodId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(paymentMethodId))
        {
            return PassengerPaymentMethodErrors.NotReusableCard;
        }

        try
        {
            var pmService = new PaymentMethodService();
            var pm = await pmService.GetAsync(paymentMethodId, cancellationToken: ct);

            if (pm.Card is null)
            {
                // Only reusable cards (incl. those behind Apple/Google Pay) are supported for saving.
                return PassengerPaymentMethodErrors.NotReusableCard;
            }

            return new StripePaymentMethodDetails(
                pm.Card.Brand ?? "card",
                pm.Card.Last4 ?? "0000",
                (int)pm.Card.ExpMonth,
                (int)pm.Card.ExpYear,
                pm.BillingDetails?.Name);
        }
        catch (StripeException ex)
        {
            this.logger.LogWarning(ex, "Could not retrieve Stripe payment method {PaymentMethodId}", paymentMethodId);
            return PassengerPaymentMethodErrors.NotReusableCard;
        }
    }

    public async Task<Result<Success>> DetachPaymentMethodAsync(string paymentMethodId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(paymentMethodId))
        {
            return Result.Success;
        }

        try
        {
            var pmService = new PaymentMethodService();
            await pmService.DetachAsync(paymentMethodId, cancellationToken: ct);
            return Result.Success;
        }
        catch (StripeException ex)
        {
            // Best-effort: a detach failure must not block the local soft-delete.
            this.logger.LogWarning(ex, "Stripe detach failed for payment method {PaymentMethodId}", paymentMethodId);
            return Result.Success;
        }
    }

    public async Task<Result<StripeSurchargeResult>> ChargeOffSessionAsync(
        string stripeCustomerId,
        string paymentMethodId,
        decimal amount,
        string currency,
        Guid tripId,
        string kind,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            return PaymentErrors.InvalidAmount;
        }

        if (string.IsNullOrWhiteSpace(stripeCustomerId) || string.IsNullOrWhiteSpace(paymentMethodId))
        {
            return PaymentErrors.StripeInitiationFailed;
        }

        try
        {
            var intentService = new PaymentIntentService();
            var options = new PaymentIntentCreateOptions
            {
                Amount = ToMinorUnits(amount),
                Currency = currency.ToLowerInvariant(),
                Customer = stripeCustomerId,
                PaymentMethod = paymentMethodId,
                Confirm = true,
                OffSession = true,
                CaptureMethod = "automatic",
                Metadata = new Dictionary<string, string>
                {
                    ["tripId"] = tripId.ToString(),
                    ["kind"] = kind,
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
            var pi = ex.StripeError?.PaymentIntent;
            var requiresAction = string.Equals(ex.StripeError?.Code, "authentication_required", StringComparison.OrdinalIgnoreCase);
            this.logger.LogWarning(
                ex, "Off-session charge failed for trip {TripId} ({Kind}): code={Code}", tripId, kind, ex.StripeError?.Code);

            if (pi is not null)
            {
                return new StripeSurchargeResult(pi.Id, pi.Status ?? "failed", pi.LatestChargeId, requiresAction);
            }

            return PaymentErrors.StripeInitiationFailed;
        }
    }

    public async Task<Result<StripeRefundResult>> CreateRefundAsync(
        string paymentIntentId,
        decimal? refundAmount = null,
        CancellationToken ct = default,
        string? idempotencyKey = null)
    {
        try
        {
            var service = new RefundService();
            var requestOptions = string.IsNullOrWhiteSpace(idempotencyKey)
                ? null
                : new RequestOptions { IdempotencyKey = idempotencyKey };

            var refund = await service.CreateAsync(
                new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId,
                    Amount = refundAmount.HasValue ? ToMinorUnits(refundAmount.Value) : null,
                },
                requestOptions,
                cancellationToken: ct);
            var refundedAmount = FromMinorUnits(refund.Amount);
            return new StripeRefundResult(
                refund.Id,
                refundedAmount,
                (refund.Currency ?? string.Empty).ToUpperInvariant(),
                refund.Status,
                refund.PaymentIntentId,
                refund.ChargeId);
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
