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
        CancellationToken ct = default);

    Task<Result<Success>> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);

    Task<Result<StripeRefundResult>> CreateRefundAsync(string paymentIntentId, CancellationToken ct = default);
}

public sealed record StripePaymentIntentResult(string PaymentIntentId, string ClientSecret, string PublishableKey);

public sealed record StripeRefundResult(string RefundId, decimal Amount, string Currency);
