namespace Taxi.Domain.Trips;

public static class TripStatuses
{
    /// <summary>
    /// The statuses that count as a live, resumable trip occupying the
    /// passenger. <see cref="TripStatus.AwaitingPayment"/> and
    /// <see cref="TripStatus.PendingQuote"/> are excluded — those are still part
    /// of the booking flow, so a half-finished booking neither shows up as an
    /// active trip nor blocks the next one.
    /// </summary>
    public static readonly TripStatus[] Active =
    [
        TripStatus.AwaitingAdminAcceptance,
        TripStatus.Accepted,
        TripStatus.EnRoute,
        TripStatus.Arrived,
        TripStatus.InProgress,
    ];
}
