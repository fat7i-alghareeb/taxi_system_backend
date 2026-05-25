using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Trips;

public sealed class TripWaitingSession : AuditableEntity
{
    public const decimal FeePerMinute = 0.15m;

    private TripWaitingSession() { }

    private TripWaitingSession(Guid id, Guid tripId, Guid driverId)
        : base(id)
    {
        TripId = tripId;
        DriverId = driverId;
        StartedAtUtc = DateTimeOffset.UtcNow;
        CreatedAtUtc = StartedAtUtc;
    }

    public Guid TripId { get; private set; }
    public Guid DriverId { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? StoppedAtUtc { get; private set; }
    public int? Minutes { get; private set; }
    public decimal? EstimatedFee { get; private set; }
    public bool IsActive => StoppedAtUtc is null;

    public static Result<TripWaitingSession> Start(Guid id, Guid tripId, Guid driverId)
    {
        return new TripWaitingSession(id, tripId, driverId);
    }

    public Result<Success> Stop()
    {
        if (StoppedAtUtc.HasValue)
        {
            return Result.Success;
        }

        StoppedAtUtc = DateTimeOffset.UtcNow;
        Minutes = Math.Max(1, (int)Math.Ceiling((StoppedAtUtc.Value - StartedAtUtc).TotalMinutes));
        EstimatedFee = Minutes.Value * FeePerMinute;
        return Result.Success;
    }
}
