using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Common;

/// <summary>
/// Application-layer, authoritative source of the customer edit-window rules, so the
/// preview / apply / bag handlers can reject an edit up front instead of failing at commit
/// time. This is where the 5-minute window is enforced (NOT in the domain mutation methods):
/// a re-priced edit that needs a payment sheet is committed later by the Stripe webhook, and
/// re-checking a short window at that async completion would wrongly reject an edit the rider
/// already paid for. Route and party size share the same 5-minute window but keep different
/// status gates — see the individual members.
/// </summary>
public static class TripEditPolicy
{
    /// <summary>
    /// How long after booking the customer may still change the route / passengers / bags.
    /// For a scheduled ride the window instead closes this long before pickup, whichever is
    /// later, so a ride booked far in advance isn't locked the moment it's created.
    /// </summary>
    private static readonly TimeSpan CustomerEditWindow = TimeSpan.FromMinutes(5);

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
    /// Whether the customer edit window is still open: five minutes after booking or — for a
    /// scheduled ride — five minutes before pickup, whichever is later.
    /// </summary>
    public static bool IsWithinCustomerEditWindow(Trip trip)
    {
        var bookingDeadline = trip.CreatedAtUtc.Add(CustomerEditWindow);
        var deadline = trip.ScheduledAtUtc is { } scheduledAtUtc
            ? Max(scheduledAtUtc.Subtract(CustomerEditWindow), bookingDeadline)
            : bookingDeadline;

        return DateTimeOffset.UtcNow <= deadline;

        static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a >= b ? a : b;
    }

    /// <summary>
    /// Validates a customer edit against the rules for the fields it actually touches, returning
    /// the same error the domain would. Lets the preview / apply / bag handlers reject an edit
    /// before spending a re-quote rather than failing at commit time.
    /// </summary>
    public static Error? Validate(Trip trip, bool editsStops, bool editsPartySize)
    {
        if (editsStops)
        {
            if (!IsStopsStatusEditable(trip.Status))
            {
                return TripErrors.InvalidStatus(trip.Status);
            }

            if (!IsWithinCustomerEditWindow(trip))
            {
                return TripErrors.EditWindowExpired;
            }
        }

        if (editsPartySize)
        {
            if (!CanEditPartySize(trip.Status))
            {
                return TripErrors.InvalidStatus(trip.Status);
            }

            if (!IsWithinCustomerEditWindow(trip))
            {
                return TripErrors.EditWindowExpired;
            }
        }

        return null;
    }

    /// <summary>
    /// Whether the customer may still change the passenger or bag count. Mirrors
    /// <c>Trip.IsPartySizeEditable</c>: closes as soon as the driver starts moving, because a
    /// passenger-count change can swap the assigned vehicle type. The time window is applied
    /// separately in <see cref="Validate"/>.
    /// </summary>
    public static bool CanEditPartySize(TripStatus status) =>
        status is TripStatus.AwaitingAdminAcceptance or TripStatus.Accepted;
}
