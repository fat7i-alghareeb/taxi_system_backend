namespace Taxi.Contracts.Requests.Trips;

public class UpdateTripScheduledTimeRequest
{
    public DateTimeOffset? ScheduledAtUtc { get; set; }
}
