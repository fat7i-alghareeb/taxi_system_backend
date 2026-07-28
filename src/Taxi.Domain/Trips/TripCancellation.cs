using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Trips;

public sealed class TripCancellation : AuditableEntity
{
    private TripCancellation() { }

    private TripCancellation(
        Guid id,
        Guid tripId,
        CancellationActor actor,
        CancellationReason reason,
        decimal refundPercent,
        decimal refundAmount,
        decimal cancellationFeeAmount,
        string currencyCode,
        string? note)
        : base(id)
    {
        TripId = tripId;
        Actor = actor;
        Reason = reason;
        RefundPercent = refundPercent;
        RefundAmount = refundAmount;
        CancellationFeeAmount = cancellationFeeAmount;
        CurrencyCode = currencyCode;
        Note = note;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid TripId { get; private set; }
    public CancellationActor Actor { get; private set; }
    public CancellationReason Reason { get; private set; }
    public decimal RefundPercent { get; private set; }
    public decimal RefundAmount { get; private set; }

    /// <summary>
    /// Flat fee withheld from the fare ("annuleringskosten"), 0 when the policy charged none.
    /// Carried separately from <see cref="RefundPercent"/> so a 100%-of-fare refund minus a fee
    /// stays explainable on receipts instead of collapsing into an odd percentage.
    /// </summary>
    public decimal CancellationFeeAmount { get; private set; }

    public string CurrencyCode { get; private set; } = "EUR";
    public string? Note { get; private set; }

    public static Result<TripCancellation> Create(
        Guid id,
        Guid tripId,
        CancellationActor actor,
        CancellationReason reason,
        decimal refundPercent,
        decimal refundAmount,
        decimal cancellationFeeAmount,
        string currencyCode,
        string? note)
    {
        if (refundPercent < 0 || refundPercent > 100 || refundAmount < 0 || cancellationFeeAmount < 0)
        {
            return TripErrors.InvalidCancellationReason;
        }

        return new TripCancellation(
            id,
            tripId,
            actor,
            reason,
            refundPercent,
            refundAmount,
            cancellationFeeAmount,
            currencyCode,
            note);
    }
}
