using Taxi.Domain.Payments;

namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Splits a refund of a trip's fare across ALL captured fare payments — the wallet-funded
/// portion first (ledger reversal), then the card portion (Stripe) — issuing one refund
/// request per source. This is required for mixed (wallet + card) trips, where a single
/// <c>PaymentKind.Fare</c> row only covers part of the fare: refunding the whole amount
/// against one payment either fails (<c>RefundExceedsAvailable</c>) or leaves the other
/// portion un-refunded. Wallet-only trips are refunded via the wallet ledger (no Stripe
/// intent needed), so this also covers cases the old single-payment sites skipped.
/// </summary>
public interface ITripRefundSplitter
{
    Task<TripRefundSplitResult> RefundAsync(TripRefundSplitRequest request, CancellationToken ct = default);
}

public sealed record TripRefundSplitRequest(
    Guid TripId,
    decimal TotalAmount,
    PaymentRefundSourceType SourceType,
    Guid? PassengerId = null,
    decimal? RefundPercent = null,
    Guid? TripCancellationId = null,
    Guid? CustomerIncidentId = null,
    Guid? TripCompensationClaimId = null,
    Guid? RequestedByAdminId = null,
    string? AdminNote = null);

public sealed record TripRefundSplitResult(
    IReadOnlyList<PaymentRefund> Refunds,
    decimal TotalRefunded)
{
    public static readonly TripRefundSplitResult Empty = new([], 0m);

    /// <summary>The first refund created — used by callers that surface a single refund on their DTO.</summary>
    public PaymentRefund? Primary => Refunds.Count > 0 ? Refunds[0] : null;

    public bool AnyCreated => Refunds.Count > 0;

    public bool AnyFailed => Refunds.Any(r => r.Status == PaymentRefundStatus.Failed);
}
