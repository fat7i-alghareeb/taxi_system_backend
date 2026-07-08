using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Common;

/// <summary>
/// Computes a trip's money breakdown from its payments, refunds and waiting sessions.
/// Shared by the customer receipt and the admin trip-financials view so both always agree.
/// </summary>
public static class TripFinancialsCalculator
{
    public static TripFinancialsDto Compute(
        decimal fareAmount,
        string currency,
        IReadOnlyCollection<Payment> payments,
        IReadOnlyCollection<PaymentRefund> refunds,
        IReadOnlyCollection<TripWaitingSession> waitingSessions)
    {
        var waitingFee = waitingSessions.Sum(s => s.EstimatedFee ?? 0m);
        var totalCharged = Round(fareAmount + waitingFee);

        var walletPaid = payments
            .Where(p => p.Method == PaymentMethod.Wallet && p.Status == PaymentStatus.Completed)
            .Sum(p => p.Amount);
        var cardPaid = payments
            .Where(p => p.Method == PaymentMethod.CreditCard && p.Status == PaymentStatus.Completed)
            .Sum(p => p.Amount);
        var totalPaid = Round(walletPaid + cardPaid);
        var unpaid = Math.Max(0m, Round(totalCharged - totalPaid));
        var refunded = refunds
            .Where(r => r.Status == PaymentRefundStatus.Succeeded)
            .Sum(r => r.Amount);

        var paymentLines = payments
            .OrderBy(p => p.CreatedAtUtc)
            .Select(p => new TripPaymentLineDto(
                p.Kind.ToString(),
                p.Method.ToString(),
                p.Amount,
                p.Status.ToString(),
                ToOffset(p.ProcessedAtUtc),
                p.StripePaymentMethodType))
            .ToList();

        var refundLines = refunds
            .OrderByDescending(r => r.RequestedAtUtc)
            .Select(r => new TripRefundLineDto(
                r.Amount,
                r.Status.ToString(),
                r.SourceType.ToString(),
                r.RequestedAtUtc,
                r.CompletedAtUtc))
            .ToList();

        return new TripFinancialsDto(
            currency,
            Round(fareAmount),
            Round(waitingFee),
            totalCharged,
            Round(walletPaid),
            Round(cardPaid),
            totalPaid,
            unpaid,
            Round(refunded),
            paymentLines,
            refundLines);
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static DateTimeOffset? ToOffset(DateTime? value) =>
        value.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : null;
}
