namespace Taxi.Domain.Trips;

public enum ScheduledTripReminderStage
{
    Unaccepted60Minutes,
    Unaccepted30Minutes,
    Unaccepted15Minutes,
    UnacceptedOverdue,
    Accepted30Minutes,
    Accepted15Minutes,

    // Customer-facing countdown reminders. Unlike the stages above they do not
    // depend on whether an admin has accepted the trip yet — the passenger was
    // promised "30 minutes before" and "15 minutes before" at booking time, so
    // they fire on the clock alone.
    Customer30Minutes,
    Customer15Minutes,
}
