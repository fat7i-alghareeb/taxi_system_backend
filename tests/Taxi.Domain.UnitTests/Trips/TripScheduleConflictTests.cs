using Taxi.Domain.Trips;
using Xunit;

namespace Taxi.Domain.UnitTests.Trips;

public class TripScheduleConflictTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 6, 20, 12, 0, 0, TimeSpan.Zero);

    /// A booking is checked against stored trips through their active windows.
    private static bool Conflicts(
        DateTimeOffset? newScheduledAt,
        DateTimeOffset? existingScheduledAt,
        DateTimeOffset? existingCreatedAt = null)
    {
        var newWindowStart = TripScheduleConflict.ActiveWindowStart(newScheduledAt, Now);
        return TripScheduleConflict.ConflictsWith(
            newWindowStart,
            existingScheduledAt,
            existingCreatedAt ?? Now);
    }

    [Fact]
    public void ScheduledTripsTwentyMinutesApart_Conflict()
    {
        Assert.True(Conflicts(Now.AddHours(2), Now.AddHours(2).AddMinutes(20)));
    }

    [Fact]
    public void ScheduledTripsThreeHoursApart_DoNotConflict()
    {
        Assert.False(Conflicts(Now.AddHours(2), Now.AddHours(5)));
    }

    [Fact]
    public void ImmediateBooking_ConflictsWithAReservationThirtyMinutesOut()
    {
        // The reservation goes live at pickup - 15 min, i.e. 15 minutes from now,
        // while the immediate ride would still be running.
        Assert.True(Conflicts(null, Now.AddMinutes(30)));
    }

    [Fact]
    public void ImmediateBooking_DoesNotConflictWithAReservationThreeHoursOut()
    {
        Assert.False(Conflicts(null, Now.AddHours(3)));
    }

    [Fact]
    public void ImmediateBooking_ConflictsWithAnExistingImmediateTrip()
    {
        Assert.False(
            Conflicts(null, null, Now.AddHours(-3)),
            "a ride created three hours ago is outside the guard");
        Assert.True(Conflicts(null, null, Now.AddMinutes(-5)));
    }

    [Fact]
    public void TheGuardIsSymmetric()
    {
        // Ordering must not matter: booking before or after an existing trip is
        // the same collision.
        Assert.Equal(
            Conflicts(Now.AddHours(2), Now.AddHours(2).AddMinutes(30)),
            Conflicts(Now.AddHours(2).AddMinutes(30), Now.AddHours(2)));
    }

    [Fact]
    public void ExactlyOneHourApart_DoesNotConflict()
    {
        // The guard is exclusive, so back-to-back rides an hour apart are allowed.
        Assert.False(Conflicts(Now.AddHours(2), Now.AddHours(3)));
    }

    [Fact]
    public void ActiveWindowStart_UsesTheDispatchLeadForScheduledTrips()
    {
        var scheduledAt = Now.AddHours(2);

        Assert.Equal(
            scheduledAt - Trip.ScheduledEnRouteLeadTime,
            TripScheduleConflict.ActiveWindowStart(scheduledAt, Now));
    }

    [Fact]
    public void ActiveWindowStart_FallsBackForImmediateTrips()
    {
        Assert.Equal(Now, TripScheduleConflict.ActiveWindowStart(null, Now));
    }
}
