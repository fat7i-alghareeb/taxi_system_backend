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
}
