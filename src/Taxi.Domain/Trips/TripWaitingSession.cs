using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Trips;

public sealed class TripWaitingSession : AuditableEntity
{
    /// <summary>Free waiting window (minutes) for a regular trip after the driver arrives.</summary>
    public const int DefaultGraceMinutes = 10;

    /// <summary>Free waiting window (minutes) for an airport trip after the agreed arrival.</summary>
    public const int AirportGraceMinutes = 30;

    /// <summary>Fallback per-minute rate used when the vehicle type rate is unavailable.</summary>
    public const decimal DefaultFeePerMinute = 0.15m;

    private TripWaitingSession() { }

    private TripWaitingSession(
        Guid id,
        Guid tripId,
        Guid driverId,
        decimal ratePerMinute,
        int graceMinutes,
        DateTimeOffset startedAtUtc)
        : base(id)
    {
        TripId = tripId;
        DriverId = driverId;
        RatePerMinute = ratePerMinute;
        GraceMinutes = graceMinutes;
        StartedAtUtc = startedAtUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Free waiting window (in minutes) for this session. Only time beyond this grace
    /// period is billable. Captured at start (10 for regular, 30 for airport trips).
    /// </summary>
    public int GraceMinutes { get; private set; } = DefaultGraceMinutes;

    public Guid TripId { get; private set; }
    public Guid DriverId { get; private set; }

    /// <summary>Per-minute waiting fee, captured from the trip's vehicle type at start.</summary>
    public decimal RatePerMinute { get; private set; }

    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? StoppedAtUtc { get; private set; }

    /// <summary>Total minutes waited (including the free grace period).</summary>
    public int? Minutes { get; private set; }

    /// <summary>Minutes actually charged for (total minus the grace period).</summary>
    public int? BillableMinutes { get; private set; }

    public decimal? EstimatedFee { get; private set; }
    public bool IsActive => StoppedAtUtc is null;

    public static Result<TripWaitingSession> Start(
        Guid id,
        Guid tripId,
        Guid driverId,
        decimal ratePerMinute,
        int graceMinutes = DefaultGraceMinutes,
        DateTimeOffset? startedAtUtc = null)
    {
        var rate = ratePerMinute > 0 ? ratePerMinute : DefaultFeePerMinute;
        var grace = graceMinutes > 0 ? graceMinutes : DefaultGraceMinutes;
        return new TripWaitingSession(
            id,
            tripId,
            driverId,
            rate,
            grace,
            startedAtUtc ?? DateTimeOffset.UtcNow);
    }

    public Result<Success> Stop()
    {
        if (StoppedAtUtc.HasValue)
        {
            return Result.Success;
        }

        StoppedAtUtc = DateTimeOffset.UtcNow;
        var totalMinutes = Math.Max(0, (int)Math.Ceiling((StoppedAtUtc.Value - StartedAtUtc).TotalMinutes));
        Minutes = totalMinutes;
        BillableMinutes = Math.Max(0, totalMinutes - GraceMinutes);
        EstimatedFee = BillableMinutes.Value * RatePerMinute;
        return Result.Success;
    }
}
