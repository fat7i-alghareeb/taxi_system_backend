using Taxi.Domain.Common;

namespace Taxi.Domain.Trips;

public enum PendingTripEditKind
{
    Stops,
    Passenger
}

public enum PendingTripEditStatus
{
    Pending,
    Applied,
    Cancelled,
    Expired
}

/// <summary>
/// A destination / passenger edit whose fare increase could not be settled silently,
/// so it is held pending an interactive PaymentSheet. The trip is NOT mutated until the
/// delta PaymentIntent succeeds (webhook applies it); it is cancelled/expired otherwise,
/// so the change reverts. Carries everything needed to apply the edit later.
/// </summary>
public sealed class PendingTripEdit : AuditableEntity
{
    private PendingTripEdit() { }

    private PendingTripEdit(
        Guid id,
        Guid tripId,
        Guid passengerId,
        PendingTripEditKind kind,
        string? proposedStopsJson,
        int? proposedPassengerCount,
        Guid newQuoteId,
        Guid? newVehicleTypeId,
        decimal deltaAmount,
        string currency,
        string stripePaymentIntentId,
        DateTimeOffset expiresAtUtc,
        decimal walletDebitedAmount,
        Guid? walletPaymentId)
        : base(id)
    {
        TripId = tripId;
        PassengerId = passengerId;
        Kind = kind;
        ProposedStopsJson = proposedStopsJson;
        ProposedPassengerCount = proposedPassengerCount;
        NewQuoteId = newQuoteId;
        NewVehicleTypeId = newVehicleTypeId;
        DeltaAmount = deltaAmount;
        Currency = currency;
        StripePaymentIntentId = stripePaymentIntentId;
        Status = PendingTripEditStatus.Pending;
        ExpiresAtUtc = expiresAtUtc;
        WalletDebitedAmount = walletDebitedAmount;
        WalletPaymentId = walletPaymentId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid TripId { get; private set; }
    public Guid PassengerId { get; private set; }
    public PendingTripEditKind Kind { get; private set; }

    /// <summary>Proposed stops as JSON (`[{latitude,longitude,label}]`) — used to rebuild the trip stops on apply.</summary>
    public string? ProposedStopsJson { get; private set; }
    public int? ProposedPassengerCount { get; private set; }
    public Guid NewQuoteId { get; private set; }
    public Guid? NewVehicleTypeId { get; private set; }
    public decimal DeltaAmount { get; private set; }
    public string Currency { get; private set; } = default!;
    public string StripePaymentIntentId { get; private set; } = default!;
    public PendingTripEditStatus Status { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    /// <summary>Wallet amount already debited toward this delta (reversed on revert). 0 when card/sheet covers all.</summary>
    public decimal WalletDebitedAmount { get; private set; }

    /// <summary>The wallet-funded fare-adjustment payment to reverse on revert, if any.</summary>
    public Guid? WalletPaymentId { get; private set; }

    public static PendingTripEdit Create(
        Guid id,
        Guid tripId,
        Guid passengerId,
        PendingTripEditKind kind,
        string? proposedStopsJson,
        int? proposedPassengerCount,
        Guid newQuoteId,
        Guid? newVehicleTypeId,
        decimal deltaAmount,
        string currency,
        string stripePaymentIntentId,
        DateTimeOffset expiresAtUtc,
        decimal walletDebitedAmount = 0m,
        Guid? walletPaymentId = null) =>
        new(id, tripId, passengerId, kind, proposedStopsJson, proposedPassengerCount,
            newQuoteId, newVehicleTypeId, deltaAmount, currency, stripePaymentIntentId, expiresAtUtc,
            walletDebitedAmount, walletPaymentId);

    public void MarkApplied() => Status = PendingTripEditStatus.Applied;

    public void MarkCancelled() => Status = PendingTripEditStatus.Cancelled;

    public void MarkExpired() => Status = PendingTripEditStatus.Expired;
}
