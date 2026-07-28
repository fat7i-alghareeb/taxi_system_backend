namespace Taxi.Domain.Trips;

/// <summary>
/// Single source of truth for the passenger/admin cancellation policy thresholds.
/// Centralizing them here keeps the backend handlers, tests, and (via client config)
/// the customer-facing copy aligned instead of duplicating magic numbers.
/// Waiting-period grace minutes live on <see cref="TripWaitingSession"/>.
/// </summary>
public static class CancellationPolicy
{
    /// <summary>
    /// Refund percent when cancelling inside the window. The fare is refunded in full and the
    /// flat <see cref="WithinWindowFeeAmount"/> is then deducted, so this stays 100 and the
    /// deduction is carried separately — see <see cref="ApplyWithinWindowFee"/>.
    /// </summary>
    public const int WithinWindowRefundPercent = 100;

    /// <summary>
    /// Flat administrative cancellation fee ("annuleringskosten") deducted when a passenger
    /// cancels inside <see cref="FreeWindowFromBooking"/>. The window is no longer free: the
    /// passenger gets the fare back minus this amount.
    /// </summary>
    public const decimal WithinWindowFeeAmount = 6.50m;

    /// <summary>Refund when a passenger cancels after the 5-minute free window has closed.</summary>
    public const int AfterWindowRefundPercent = 45;

    /// <summary>Refund issued to the passenger when a driver/admin cancels due to passenger no-show.</summary>
    public const int DriverCancelRefundPercent = 45;

    /// <summary>
    /// Early-cancellation grace measured from booking creation time.
    /// Applies to all trip types (immediate and scheduled). Inside this window the passenger pays
    /// only <see cref="WithinWindowFeeAmount"/>; after it, they receive
    /// <see cref="AfterWindowRefundPercent"/> of the fare.
    /// The name is kept for continuity — the window is no longer free.
    /// </summary>
    public static readonly TimeSpan FreeWindowFromBooking = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether a passenger cancellation still falls inside the early window at <paramref name="now"/>.
    /// The window is inclusive: exactly at the boundary still counts as inside.
    /// </summary>
    public static bool IsWithinFreeWindow(
        DateTimeOffset createdAtUtc,
        DateTimeOffset now)
    {
        return now <= createdAtUtc + FreeWindowFromBooking;
    }

    /// <summary>
    /// Splits <paramref name="fare"/> for an in-window passenger cancellation.
    /// The fee is always the full <see cref="WithinWindowFeeAmount"/> — it is never capped to the
    /// fare — so a fare below the fee refunds nothing and leaves a <c>Shortfall</c> the caller must
    /// still collect (as wallet debt). Handlers and tests share this one formula.
    /// </summary>
    public static (decimal Fee, decimal Refund, decimal Shortfall) ApplyWithinWindowFee(decimal fare)
    {
        var fee = WithinWindowFeeAmount;
        var refund = Math.Round(Math.Max(0m, fare - fee), 2, MidpointRounding.AwayFromZero);
        var shortfall = Math.Round(Math.Max(0m, fee - fare), 2, MidpointRounding.AwayFromZero);

        return (fee, refund, shortfall);
    }
}
