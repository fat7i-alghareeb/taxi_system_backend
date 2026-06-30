using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Refunds.Dtos;
using Taxi.Domain.Payments;

namespace Taxi.Application.Features.Refunds;

internal static class RefundDtoProjector
{
    public static async Task<List<AdminRefundDetailDto>> ToAdminRefundDetailsAsync(
        IAppDbContext context,
        IReadOnlyCollection<PaymentRefund> refunds,
        CancellationToken ct)
    {
        if (refunds.Count == 0)
        {
            return [];
        }

        var paymentIds = refunds.Select(refund => refund.PaymentId).Distinct().ToList();
        var payments = await context.Payments
            .AsNoTracking()
            .Where(payment => paymentIds.Contains(payment.Id))
            .ToDictionaryAsync(payment => payment.Id, ct);

        var allPaymentRefunds = await context.PaymentRefunds
            .AsNoTracking()
            .Where(refund => paymentIds.Contains(refund.PaymentId))
            .ToListAsync(ct);

        return refunds.Select(refund =>
        {
            payments.TryGetValue(refund.PaymentId, out var payment);
            var capturedAmount = payment?.Amount ?? refund.OriginalPaymentAmountSnapshot;
            var totals = PaymentRefundAccounting.Calculate(
                capturedAmount,
                allPaymentRefunds.Where(item => item.PaymentId == refund.PaymentId));

            return new AdminRefundDetailDto(
                refund.Id,
                refund.PaymentId,
                refund.TripId,
                refund.PassengerId,
                refund.Status.ToString(),
                refund.SourceType.ToString(),
                refund.Amount,
                refund.Currency,
                refund.RefundPercent,
                refund.IsFullRefund,
                refund.OriginalPaymentAmountSnapshot,
                totals.SuccessfulAmount,
                totals.AvailableAmount,
                payment?.Method.ToString() ?? string.Empty,
                refund.RequestedAtUtc,
                refund.LastAttemptAtUtc,
                refund.CompletedAtUtc,
                refund.FailedAtUtc,
                refund.StripeRefundId,
                refund.StripePaymentIntentId,
                refund.StripeChargeId,
                refund.FailureCode,
                refund.FailureReason,
                refund.AttemptCount,
                refund.CanRetry,
                refund.RetryBlockedReason,
                refund.RequiresAdminAction,
                refund.TripCancellationId,
                refund.CustomerIncidentId,
                refund.TripCompensationClaimId,
                refund.RequestedByAdminId,
                refund.AdminNote);
        }).ToList();
    }
}
