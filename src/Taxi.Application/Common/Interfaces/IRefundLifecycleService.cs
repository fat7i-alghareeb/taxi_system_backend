using Taxi.Application.Features.Payments.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;

namespace Taxi.Application.Common.Interfaces;

public interface IRefundLifecycleService
{
    Task<Result<PaymentRefund>> RequestRefundAsync(RefundRequest request, CancellationToken ct = default);

    Task<Result<PaymentRefund>> RetryRefundAsync(Guid refundId, Guid adminId, string? note, CancellationToken ct = default);

    // Checks a Pending refund directly against Stripe and resolves it if Stripe now reports an
    // outcome. No-op if the refund isn't Pending or has no StripeRefundId yet. Used by the
    // background reconciliation job to recover refunds whose confirming webhook never arrived.
    Task<Result<PaymentRefund>> ReconcilePendingRefundAsync(Guid refundId, CancellationToken ct = default);

    Task<Result<RefundableBalanceResult>> GetRefundableBalanceAsync(Guid paymentId, CancellationToken ct = default);

    Task<Result<RefundSummaryDto?>> BuildCustomerRefundSummaryAsync(
        Guid tripId,
        Guid? paymentId = null,
        CancellationToken ct = default);

    Task<Result<AdminRefundSummaryDto?>> BuildAdminRefundSummaryAsync(
        Guid refundId,
        Guid? tripId = null,
        CancellationToken ct = default);
}

public sealed record RefundRequest(
    Guid PaymentId,
    decimal? Amount,
    PaymentRefundSourceType SourceType,
    decimal? RefundPercent = null,
    bool IsFullRefund = false,
    Guid? TripId = null,
    Guid? TripCancellationId = null,
    Guid? CustomerIncidentId = null,
    Guid? TripCompensationClaimId = null,
    Guid? RequestedByAdminId = null,
    Guid? PassengerId = null,
    string? AdminNote = null);

public sealed record RefundableBalanceResult(
    Guid PaymentId,
    decimal CapturedAmount,
    decimal SuccessfulRefundedAmount,
    decimal ReservedRefundAmount,
    decimal FailedRefundAmount,
    decimal AvailableRefundAmount,
    bool IsFullyRefunded);
