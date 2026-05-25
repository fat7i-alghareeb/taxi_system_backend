using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Interfaces;

public interface IStripePaymentService
{
    Task<Result<StripePaymentIntentResult>> CreatePaymentIntentAsync(
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
        CancellationToken ct = default);

    Task<Result<Success>> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);

    Task<Result<StripeRefundResult>> CreateRefundAsync(string paymentIntentId, decimal? amount = null, CancellationToken ct = default);
}

public sealed record StripePaymentIntentResult(
    string PaymentIntentId,
    string ClientSecret,
    string PublishableKey,
    string CustomerId,
    string EphemeralKeySecret);

public sealed record StripeRefundResult(string RefundId, decimal Amount, string Currency);
