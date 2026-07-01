namespace Taxi.Domain.Trips;

/// <summary>
/// Single source of truth for the passenger/admin cancellation policy thresholds.
/// Centralizing them here keeps the backend handlers, tests, and (via client config)
/// the customer-facing copy aligned instead of duplicating magic numbers.
/// Waiting-period grace minutes live on <see cref="TripWaitingSession"/>.
/// </summary>
public static class CancellationPolicy
{
    /// <summary>Refund when cancelling inside the free window.</summary>
    public const int WithinWindowRefundPercent = 100;

    /// <summary>Refund when a passenger cancels after the 5-minute free window has closed.</summary>
    public const int AfterWindowRefundPercent = 45;

    /// <summary>Refund issued to the passenger when a driver/admin cancels due to passenger no-show.</summary>
    public const int DriverCancelRefundPercent = 45;

    /// <summary>
    /// Free-cancellation grace measured from booking creation time.
    /// Applies to all trip types (immediate and scheduled). After this window,
    /// the passenger receives <see cref="AfterWindowRefundPercent"/> of the fare.
    /// </summary>
    public static readonly TimeSpan FreeWindowFromBooking = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether a passenger cancellation is still free at <paramref name="now"/>.
    /// Free if cancellation occurs within <see cref="FreeWindowFromBooking"/> of booking creation.
    /// The window is inclusive: exactly at the boundary is still free.
    /// </summary>
    public static bool IsWithinFreeWindow(
        DateTimeOffset createdAtUtc,
        DateTimeOffset now)
    {
        return now <= createdAtUtc + FreeWindowFromBooking;
    }
}
