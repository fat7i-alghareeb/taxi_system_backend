using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Common;

/// <summary>
/// Application-layer, authoritative and ONLY source of the customer edit-window rule: every
/// customer-initiated edit — route, stops, party size, bags, scheduled time — is allowed for
/// exactly five minutes from booking creation and no longer. There is deliberately no
/// scheduled-pickup anchoring: a ride booked days ahead locks at the same five-minute mark as an
/// immediate one, so the customer sees one rule instead of two.
///
/// The window is enforced here and NOT in the <c>Trip</c> mutation methods: a re-priced edit that
/// needs a payment sheet is committed later by the Stripe webhook (see
/// <c>HandleStripeWebhookCommandHandler</c> → <c>TripEditApplier</c>), and re-checking a
/// five-minute window at that async completion would reject an edit the rider already paid for.
/// The domain keeps only the status gates, which are lifecycle invariants and safe to re-evaluate
/// at any time.
///
/// The status gate still differs per field — see the individual members.
/// </summary>
public static class TripEditPolicy
{
    /// <summary>
    /// How long after booking the customer may still change anything about the trip. Measured
    /// from <c>CreatedAtUtc</c> for every trip type; a scheduled pickup does not extend it.
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
    /// Whether the customer edit window is still open: five minutes from booking creation, for
    /// every trip type. Inclusive at the boundary, matching
    /// <see cref="Taxi.Domain.Trips.CancellationPolicy.IsWithinFreeWindow"/> — the two windows are
    /// numerically identical today but remain independent product decisions, so neither delegates
    /// to the other.
    /// </summary>
    public static bool IsWithinCustomerEditWindow(Trip trip) =>
        DateTimeOffset.UtcNow <= trip.CreatedAtUtc.Add(CustomerEditWindow);

    /// <summary>
    /// Validates a customer edit against the rules for the fields it actually touches, returning
    /// the same error the domain would. Lets the preview / apply / bag handlers reject an edit
    /// before spending a re-quote rather than failing at commit time.
    /// </summary>
    public static Error? Validate(
        Trip trip,
        bool editsStops,
        bool editsPartySize,
        bool editsSchedule = false)
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

        // Kept as its own block rather than hoisting one shared window check to the top: hoisting
        // would flip the error precedence, so an en-route trip edited past the window would report
        // EditWindowExpired where it reports InvalidStatus today.
        if (editsSchedule)
        {
            if (!CanEditSchedule(trip.Status))
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

    /// <summary>
    /// Whether the customer may still move the scheduled pickup. Mirrors the status gate in
    /// <c>Trip.UpdateScheduledTime</c>: only before a driver is dispatched, since the scheduled
    /// time drives the dispatch window. Kept separate from <see cref="CanEditPartySize"/> even
    /// though the two sets match today, so either can move without a silent side effect. The time
    /// window is applied separately in <see cref="Validate"/>.
    /// </summary>
    public static bool CanEditSchedule(TripStatus status) =>
        status is TripStatus.AwaitingAdminAcceptance or TripStatus.Accepted;
}
