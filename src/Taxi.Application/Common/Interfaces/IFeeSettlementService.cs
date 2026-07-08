using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Settles a ride-related fee using the confirmed order: wallet balance first, then the
/// passenger's default saved reusable card for the remainder, and whatever cannot be collected
/// is left Unpaid (surfaced via the trip's outstanding fee). Every portion is recorded so the
/// invoice and dashboards can show the breakdown, and the whole operation is idempotent.
/// </summary>
public interface IFeeSettlementService
{
    Task<Result<FeeSettlementOutcome>> SettleWaitingFeeAsync(
        Guid tripId,
        Guid passengerId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken ct = default);
}

public sealed record FeeSettlementOutcome(
    decimal Amount,
    decimal WalletPaid,
    decimal CardPaid,
    decimal Unpaid,
    string Currency)
{
    public bool HasUnpaid => Unpaid > 0m;
}
