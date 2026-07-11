using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Payments;

namespace Taxi.Application.Features.Payments.Services;

/// <inheritdoc cref="ITripRefundSplitter" />
public sealed class TripRefundSplitter(
    IAppDbContext context,
    IRefundLifecycleService refundLifecycle) : ITripRefundSplitter
{
    public async Task<TripRefundSplitResult> RefundAsync(
        TripRefundSplitRequest request,
        CancellationToken ct = default)
    {
        var remaining = decimal.Round(request.TotalAmount, 2, MidpointRounding.AwayFromZero);
        if (remaining <= 0m)
        {
            return TripRefundSplitResult.Empty;
        }

        var payments = await context.Payments
            .Where(p => p.TripId == request.TripId
                && p.Status == PaymentStatus.Completed
                && (p.Kind == PaymentKind.Fare || p.Kind == PaymentKind.FareAdjustment))
            .ToListAsync(ct);

        // Wallet money is reversed to the wallet; card money to the original card. Wallet first,
        // then oldest-to-newest so the split is deterministic.
        var ordered = payments
            .OrderBy(p => p.Method == PaymentMethod.Wallet ? 0 : 1)
            .ThenBy(p => p.CreatedAtUtc)
            .ToList();

        var created = new List<PaymentRefund>();
        var totalRefunded = 0m;

        foreach (var payment in ordered)
        {
            if (remaining <= 0m)
            {
                break;
            }

            var balance = await refundLifecycle.GetRefundableBalanceAsync(payment.Id, ct);
            if (balance.IsFailure || balance.Value.AvailableRefundAmount <= 0m)
            {
                continue;
            }

            var portion = decimal.Round(
                Math.Min(remaining, balance.Value.AvailableRefundAmount), 2, MidpointRounding.AwayFromZero);
            if (portion <= 0m)
            {
                continue;
            }

            var refundResult = await refundLifecycle.RequestRefundAsync(
                new RefundRequest(
                    PaymentId: payment.Id,
                    Amount: portion,
                    SourceType: request.SourceType,
                    RefundPercent: request.RefundPercent,
                    IsFullRefund: portion >= payment.Amount,
                    TripId: request.TripId,
                    TripCancellationId: request.TripCancellationId,
                    CustomerIncidentId: request.CustomerIncidentId,
                    TripCompensationClaimId: request.TripCompensationClaimId,
                    RequestedByAdminId: request.RequestedByAdminId,
                    PassengerId: request.PassengerId,
                    AdminNote: request.AdminNote),
                ct);

            if (refundResult.IsFailure)
            {
                // Could not even record the refund for this source — skip and try the next.
                continue;
            }

            created.Add(refundResult.Value);
            remaining -= portion;
            if (refundResult.Value.Status != PaymentRefundStatus.Failed)
            {
                totalRefunded += portion;
            }
        }

        return new TripRefundSplitResult(created, decimal.Round(totalRefunded, 2, MidpointRounding.AwayFromZero));
    }
}
