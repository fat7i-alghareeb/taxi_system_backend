namespace Taxi.Domain.Trips;

public enum ScheduledTripReminderStage
{
    Unaccepted60Minutes,
    Unaccepted30Minutes,
    Unaccepted15Minutes,
    UnacceptedOverdue,
    Accepted30Minutes,
    Accepted15Minutes,
}
