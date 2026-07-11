using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Settles a mid-trip fare INCREASE silently: wallet balance first, then the
/// passenger's default saved reusable card (off-session), then the reusable card on
/// the original fare intent. Unlike a waiting fee, an uncovered remainder is NOT left
/// Unpaid — it is reported via <see cref="FareAdjustmentSettlementOutcome.RequiresInteractiveSheet"/>
/// so the caller can fall back to an interactive Stripe PaymentSheet (the edit stays
/// pending until that succeeds). Idempotent.
/// </summary>
public interface IFareAdjustmentSettlementService
{
    Task<Result<FareAdjustmentSettlementOutcome>> SettleAsync(
        Guid tripId,
        Guid passengerId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken ct = default);
}

public sealed record FareAdjustmentSettlementOutcome(
    decimal Amount,
    decimal WalletPaid,
    decimal CardPaid,
    decimal Remaining,
    Guid? WalletPaymentId = null)
{
    /// <summary>True when wallet + off-session card couldn't cover it — needs a PaymentSheet.</summary>
    public bool RequiresInteractiveSheet => Remaining > 0m;
}
