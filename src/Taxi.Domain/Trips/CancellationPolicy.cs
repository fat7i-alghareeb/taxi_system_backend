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

    /// <summary>Refund when a passenger cancels after the free window has closed.</summary>
    public const int AfterWindowRefundPercent = 20;

    /// <summary>Refund issued to the passenger on a driver/admin no-show cancellation.</summary>
    public const int DriverCancelRefundPercent = 20;

    /// <summary>
    /// Flat fee charged when a passenger cancels after the driver has already arrived at
    /// the pickup point (regardless of the 1-hour booking window).
    /// The passenger is refunded the remaining fare after this fee is deducted.
    /// </summary>
    public const decimal ArrivedCancellationFee = 6.50m;

    /// <summary>
    /// Free-cancellation grace measured from booking time. Applies to immediate trips
    /// and also protects a scheduled trip that was just booked.
    /// </summary>
    public static readonly TimeSpan FreeWindowFromBooking = TimeSpan.FromHours(1);

    /// <summary>
    /// For scheduled trips, cancellation stays free until this lead time before the
    /// agreed pickup, regardless of when the booking was made.
    /// </summary>
    public static readonly TimeSpan ScheduledFreeWindowLeadTime = TimeSpan.FromHours(1);

    /// <summary>
    /// Whether a passenger cancellation is still free at <paramref name="now"/>.
    /// Free if it is within the booking grace OR (for scheduled trips) still earlier
    /// than the lead-time cutoff before the scheduled pickup.
    /// </summary>
    public static bool IsWithinFreeWindow(
        DateTimeOffset createdAtUtc,
        DateTimeOffset? scheduledAtUtc,
        DateTimeOffset now)
    {
        if (now <= createdAtUtc + FreeWindowFromBooking)
        {
            return true;
        }

        return scheduledAtUtc is { } scheduledAt
            && now <= scheduledAt - ScheduledFreeWindowLeadTime;
    }
}
