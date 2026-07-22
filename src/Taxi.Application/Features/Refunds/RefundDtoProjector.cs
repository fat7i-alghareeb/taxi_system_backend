using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Refunds.Dtos;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Refunds;

public static class RefundDtoProjector
{
    public static async Task<List<AdminRefundDetailDto>> ToAdminRefundDetailsAsync(
        IAppDbContext context,
        IReadOnlyCollection<PaymentRefund> refunds,
        CancellationToken ct,
        bool forceRetryForFailedRefunds = false)
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

            var canRetry = refund.CanRetry ||
                (forceRetryForFailedRefunds &&
                    refund.Status is PaymentRefundStatus.Failed or PaymentRefundStatus.RequiresAdminAction);

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
                canRetry,
                refund.RetryBlockedReason,
                refund.RequiresAdminAction,
                refund.TripCancellationId,
                refund.CustomerIncidentId,
                refund.TripCompensationClaimId,
                refund.RequestedByAdminId,
                refund.AdminNote,
                refund.LastReconciledAtUtc,
                false,
                null);
        }).ToList();
    }

    public static async Task<List<AdminRefundDetailDto>> ToManualCancellationRefundDetailsAsync(
        IAppDbContext context,
        IReadOnlyCollection<TripCancellation> cancellations,
        CancellationToken ct)
    {
        if (cancellations.Count == 0)
        {
            return [];
        }

        var tripIds = cancellations.Select(cancellation => cancellation.TripId).Distinct().ToList();
        var trips = await context.Trips
            .AsNoTracking()
            .Where(trip => tripIds.Contains(trip.Id))
            .ToDictionaryAsync(trip => trip.Id, ct);

        var paymentRows = await context.Payments
            .AsNoTracking()
            .Where(payment => tripIds.Contains(payment.TripId))
            .ToListAsync(ct);

        var payments = paymentRows
            .GroupBy(payment => payment.TripId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(payment => payment.ProcessedAtUtc)
                    .ThenByDescending(payment => payment.CreatedAtUtc)
                    .First());

        return cancellations.Select(cancellation =>
        {
            trips.TryGetValue(cancellation.TripId, out var trip);
            payments.TryGetValue(cancellation.TripId, out var payment);

            return new AdminRefundDetailDto(
                null,
                payment?.Id,
                cancellation.TripId,
                trip?.PassengerId,
                PaymentRefundStatus.Failed.ToString(),
                MapCancellationSourceType(cancellation).ToString(),
                cancellation.RefundAmount,
                cancellation.CurrencyCode,
                cancellation.RefundPercent,
                cancellation.RefundPercent >= 100m,
                payment?.Amount ?? cancellation.RefundAmount,
                0m,
                cancellation.RefundAmount,
                payment?.Method.ToString() ?? string.Empty,
                cancellation.CreatedAtUtc,
                cancellation.CreatedAtUtc,
                null,
                cancellation.CreatedAtUtc,
                null,
                null,
                null,
                PaymentErrors.RefundUnavailable.Code,
                PaymentErrors.RefundUnavailable.Description,
                1,
                false,
                PaymentErrors.RefundUnavailable.Code,
                true,
                cancellation.Id,
                null,
                null,
                null,
                cancellation.Note,
                null,
                false,
                cancellation.Reason.ToString());
        }).ToList();
    }

    public static PaymentRefundSourceType MapCancellationSourceType(TripCancellation cancellation)
        => cancellation.Reason == CancellationReason.AirportWaitDeclined
            ? PaymentRefundSourceType.AirportWaitCancellation
            : cancellation.Actor switch
            {
                CancellationActor.Admin => PaymentRefundSourceType.AdminCancellation,
                CancellationActor.Driver => PaymentRefundSourceType.DriverCancellation,
                _ => PaymentRefundSourceType.PassengerCancellation,
            };
}
