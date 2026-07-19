using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Common;

/// <summary>
/// Application-layer mirror of the customer edit rules enforced on <see cref="Trip"/>, so the
/// preview / apply handlers can reject an edit up front instead of failing at commit time.
/// Route and party size have deliberately different rules — see the individual members.
/// </summary>
public static class TripEditPolicy
{
    private static readonly TimeSpan BookingEditWindow = TimeSpan.FromHours(1);

    /// <summary>
    /// Statuses in which the route may still be repointed. Mirrors
    /// <c>Trip.IsEditableForRepricing</c>: open from booking through arrival, never once the ride
    /// is in progress or terminal.
    /// </summary>
    public static bool IsStopsStatusEditable(TripStatus status) =>
        status is TripStatus.AwaitingAdminAcceptance
            or TripStatus.Accepted
            or TripStatus.EnRoute
            or TripStatus.Arrived;

    /// <summary>
    /// Whether the route edit window is still open. Mirrors <c>Trip.IsWithinStopsEditWindow</c>:
    /// an hour after booking or — for a scheduled ride — an hour before pickup, whichever is later.
    /// </summary>
    public static bool IsWithinStopsEditWindow(Trip trip)
    {
        var bookingDeadline = trip.CreatedAtUtc.Add(BookingEditWindow);
        var deadline = trip.ScheduledAtUtc is { } scheduledAtUtc
            ? Max(scheduledAtUtc.Subtract(BookingEditWindow), bookingDeadline)
            : bookingDeadline;

        return DateTimeOffset.UtcNow <= deadline;

        static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a >= b ? a : b;
    }

    /// <summary>
    /// Validates a customer edit against the rules for the fields it actually touches, returning
    /// the same error the domain would. Lets the preview / apply handlers reject an edit before
    /// spending a re-quote rather than failing at commit time.
    /// </summary>
    public static Error? Validate(Trip trip, bool editsStops, bool editsPartySize)
    {
        if (editsStops)
        {
            if (!IsStopsStatusEditable(trip.Status))
            {
                return TripErrors.InvalidStatus(trip.Status);
            }

            if (!IsWithinStopsEditWindow(trip))
            {
                return TripErrors.EditWindowExpired;
            }
        }

        if (editsPartySize && !CanEditPartySize(trip.Status))
        {
            return TripErrors.InvalidStatus(trip.Status);
        }

        return null;
    }

    /// <summary>
    /// Whether the customer may still change the passenger or bag count. Mirrors
    /// <c>Trip.IsPartySizeEditable</c>: closes as soon as the driver starts moving, because a
    /// passenger-count change can swap the assigned vehicle type.
    /// </summary>
    public static bool CanEditPartySize(TripStatus status) =>
        status is TripStatus.AwaitingAdminAcceptance or TripStatus.Accepted;
}
