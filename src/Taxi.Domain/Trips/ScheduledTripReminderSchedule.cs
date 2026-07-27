namespace Taxi.Domain.Trips;

/// <summary>
/// Decides which scheduled-trip reminders are due at a given moment.
///
/// Lives in the domain rather than in the background service because these are
/// business rules — which audience is reminded, and how close to pickup — and
/// both audiences fall due at the same T-30 / T-15 marks.
/// </summary>
public static class ScheduledTripReminderSchedule
{
    /// <summary>
    /// Every reminder due right now. Callers must still skip the ones already
    /// sent (<see cref="Trip.HasReminderBeenSent"/>) — this method is stateless
    /// and answers "due", not "unsent".
    /// </summary>
    public static IEnumerable<ScheduledTripReminderStage> ResolveDueStages(
        TripStatus status,
        DateTimeOffset scheduledAtUtc,
        DateTimeOffset createdAtUtc,
        DateTimeOffset now)
    {
        var adminStage = ResolveAdminStage(status, scheduledAtUtc, now);
        if (adminStage is not null)
        {
            yield return adminStage.Value;
        }

        var customerStage = ResolveCustomerStage(scheduledAtUtc, createdAtUtc, now);
        if (customerStage is not null)
        {
            yield return customerStage.Value;
        }
    }

    /// <summary>
    /// The passenger was promised a 30-minute and a 15-minute heads-up at
    /// booking time, so these fire on the clock alone — independent of whether
    /// an admin has accepted the trip yet. Nothing is sent once the pickup time
    /// has passed.
    /// </summary>
    public static ScheduledTripReminderStage? ResolveCustomerStage(
        DateTimeOffset scheduledAtUtc,
        DateTimeOffset createdAtUtc,
        DateTimeOffset now)
    {
        var remaining = scheduledAtUtc - now;
        if (remaining <= TimeSpan.Zero)
        {
            return null;
        }

        // Booking lead time, not remaining time. A ride booked 20 minutes ahead
        // is already inside the 30-minute mark when it is created; firing a
        // "30 minutes before" reminder seconds after the booking confirmation
        // reads as a bug.
        var bookingLead = scheduledAtUtc - createdAtUtc;

        if (remaining <= TimeSpan.FromMinutes(15))
        {
            return bookingLead > TimeSpan.FromMinutes(15)
                ? ScheduledTripReminderStage.Customer15Minutes
                : null;
        }

        if (remaining <= TimeSpan.FromMinutes(30))
        {
            return bookingLead > TimeSpan.FromMinutes(30)
                ? ScheduledTripReminderStage.Customer30Minutes
                : null;
        }

        return null;
    }

    /// <summary>
    /// Admin preparation and escalation reminders. Unaccepted trips escalate
    /// (60 / 30 / 15 / overdue); accepted ones only get the two preparation
    /// nudges.
    /// </summary>
    public static ScheduledTripReminderStage? ResolveAdminStage(
        TripStatus status,
        DateTimeOffset scheduledAtUtc,
        DateTimeOffset now)
    {
        var remaining = scheduledAtUtc - now;

        if (status == TripStatus.AwaitingAdminAcceptance)
        {
            if (remaining <= TimeSpan.Zero)
            {
                return ScheduledTripReminderStage.UnacceptedOverdue;
            }

            if (remaining <= TimeSpan.FromMinutes(15))
            {
                return ScheduledTripReminderStage.Unaccepted15Minutes;
            }

            if (remaining <= TimeSpan.FromMinutes(30))
            {
                return ScheduledTripReminderStage.Unaccepted30Minutes;
            }

            if (remaining <= TimeSpan.FromMinutes(60))
            {
                return ScheduledTripReminderStage.Unaccepted60Minutes;
            }

            return null;
        }

        if (remaining <= TimeSpan.Zero)
        {
            return null;
        }

        if (remaining <= TimeSpan.FromMinutes(15))
        {
            return ScheduledTripReminderStage.Accepted15Minutes;
        }

        if (remaining <= TimeSpan.FromMinutes(30))
        {
            return ScheduledTripReminderStage.Accepted30Minutes;
        }

        return null;
    }
}
