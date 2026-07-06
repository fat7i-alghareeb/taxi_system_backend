using Taxi.Domain.Trips;

using Xunit;

namespace Taxi.Domain.UnitTests.Trips;

public class TripWaitingSessionTests
{
    [Fact]
    public void Start_CapturesProvidedVehicleRateForNormalTrip()
    {
        var session = TripWaitingSession.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            0.22m,
            TripWaitingSession.DefaultGraceMinutes).Value;

        Assert.Equal(0.22m, session.RatePerMinute);
        Assert.Equal(10, session.GraceMinutes);
    }

    [Fact]
    public void Start_CapturesAirportPolicyRateAndGrace()
    {
        var session = TripWaitingSession.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TripWaitingSession.DefaultFeePerMinute,
            TripWaitingSession.AirportGraceMinutes).Value;

        Assert.Equal(0.15m, session.RatePerMinute);
        Assert.Equal(30, session.GraceMinutes);
    }

    [Fact]
    public void Stop_WhenWithinGracePeriod_RecordsZeroBillableMinutesAndFee()
    {
        var startedAt = new DateTimeOffset(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);
        var stoppedAt = startedAt.AddMinutes(10);
        var session = TripWaitingSession.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            0.13m,
            TripWaitingSession.DefaultGraceMinutes,
            startedAt).Value;

        session.Stop(stoppedAt);

        Assert.Equal(10, session.Minutes);
        Assert.Equal(0, session.BillableMinutes);
        Assert.Equal(0m, session.EstimatedFee);
    }

    [Fact]
    public void Stop_WhenBeyondGracePeriod_BillsOnlyMinutesAfterGrace()
    {
        var startedAt = new DateTimeOffset(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);
        var stoppedAt = startedAt.AddMinutes(20);
        var session = TripWaitingSession.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            0.13m,
            TripWaitingSession.DefaultGraceMinutes,
            startedAt).Value;

        session.Stop(stoppedAt);

        Assert.Equal(20, session.Minutes);
        Assert.Equal(10, session.BillableMinutes);
        Assert.Equal(1.30m, session.EstimatedFee);
    }
}
