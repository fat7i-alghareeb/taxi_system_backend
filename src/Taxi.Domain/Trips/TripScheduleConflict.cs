namespace Taxi.Domain.Trips;

/// <summary>
/// Decides whether two trips would be active at the same time.
///
/// A passenger may hold any number of future reservations but only one trip may
/// ever be *active*. The comparison is on the moment each trip becomes active,
/// not on its pickup time: a scheduled trip goes live at
/// <c>pickup - ScheduledEnRouteLeadTime</c> while an immediate one goes live at
/// once, so comparing raw pickup times would miss an immediate ride booked half
/// an hour before a reservation's dispatch window opens.
/// </summary>
public static class TripScheduleConflict
{
    /// <summary>
    /// How close two active windows may start before they are treated as
    /// overlapping. A trip has no known end time when it is booked, so this
    /// stands in for "one ride's worth of time".
    /// </summary>
    public static readonly TimeSpan Guard = TimeSpan.FromMinutes(60);

    /// <summary>
    /// When a trip starts occupying the passenger. [fallbackUtc] is used for
    /// immediate trips, which have no scheduled pickup — the request time for a
    /// booking being validated, the creation time for one already stored.
    /// </summary>
    public static DateTimeOffset ActiveWindowStart(
        DateTimeOffset? scheduledAtUtc,
        DateTimeOffset fallbackUtc)
    {
        return scheduledAtUtc?.Subtract(Trip.ScheduledEnRouteLeadTime) ?? fallbackUtc;
    }

    /// <summary>Whether two active windows starting at [a] and [b] collide.</summary>
    public static bool Conflicts(DateTimeOffset a, DateTimeOffset b) =>
        (a - b).Duration() < Guard;

    /// <summary>
    /// Convenience overload for checking a booking against a stored trip.
    /// </summary>
    public static bool ConflictsWith(
        DateTimeOffset newWindowStart,
        DateTimeOffset? existingScheduledAtUtc,
        DateTimeOffset existingCreatedAtUtc)
    {
        return Conflicts(
            newWindowStart,
            ActiveWindowStart(existingScheduledAtUtc, existingCreatedAtUtc));
    }
}
