using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Settles a ride-related fee in a fixed order: wallet balance first, then the passenger's default
/// saved reusable card for the remainder, and finally — for a fee the customer cannot decline —
/// whatever is still uncollected is charged to the wallet as DEBT, taking the balance negative.
/// Every portion is recorded so the invoice and dashboards show a settled trip, and the whole
/// operation is idempotent.
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
    string Currency,
    decimal ChargedToDebt = 0m)
{
    /// <summary>Money that could not be collected AND could not be charged to debt.</summary>
    public bool HasUnpaid => Unpaid > 0m;

    /// <summary>The fee was moved to the customer's wallet as debt rather than collected.</summary>
    public bool HasDebt => ChargedToDebt > 0m;
}
