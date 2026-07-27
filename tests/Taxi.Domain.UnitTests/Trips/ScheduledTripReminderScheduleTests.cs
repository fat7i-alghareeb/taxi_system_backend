using Taxi.Domain.Trips;
using Xunit;

namespace Taxi.Domain.UnitTests.Trips;

public class ScheduledTripReminderScheduleTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 6, 20, 12, 0, 0, TimeSpan.Zero);

    /// A ride booked well ahead of time, so the booking-lead guard never
    /// suppresses a customer reminder.
    private static readonly DateTimeOffset BookedLongAgo = Now.AddDays(-1);

    [Fact]
    public void At30Minutes_YieldsBothAdminAndCustomerStages()
    {
        var stages = ScheduledTripReminderSchedule.ResolveDueStages(
            TripStatus.AwaitingAdminAcceptance,
            Now.AddMinutes(30),
            BookedLongAgo,
            Now).ToList();

        Assert.Contains(ScheduledTripReminderStage.Unaccepted30Minutes, stages);
        Assert.Contains(ScheduledTripReminderStage.Customer30Minutes, stages);
        Assert.Equal(2, stages.Count);
    }

    [Fact]
    public void At15Minutes_YieldsBothAdminAndCustomerStages()
    {
        var stages = ScheduledTripReminderSchedule.ResolveDueStages(
            TripStatus.Accepted,
            Now.AddMinutes(15),
            BookedLongAgo,
            Now).ToList();

        Assert.Contains(ScheduledTripReminderStage.Accepted15Minutes, stages);
        Assert.Contains(ScheduledTripReminderStage.Customer15Minutes, stages);
    }

    [Fact]
    public void At31Minutes_YieldsNoCustomerStage()
    {
        var stages = ScheduledTripReminderSchedule.ResolveDueStages(
            TripStatus.Accepted,
            Now.AddMinutes(31),
            BookedLongAgo,
            Now).ToList();

        Assert.DoesNotContain(ScheduledTripReminderStage.Customer30Minutes, stages);
        Assert.DoesNotContain(ScheduledTripReminderStage.Customer15Minutes, stages);
    }

    [Theory]
    [InlineData(TripStatus.AwaitingAdminAcceptance)]
    [InlineData(TripStatus.Accepted)]
    public void CustomerReminders_AreIndependentOfAdminAcceptance(TripStatus status)
    {
        var stages = ScheduledTripReminderSchedule.ResolveDueStages(
            status,
            Now.AddMinutes(30),
            BookedLongAgo,
            Now).ToList();

        Assert.Contains(ScheduledTripReminderStage.Customer30Minutes, stages);
    }

    [Fact]
    public void AfterPickupTime_YieldsNoCustomerStage()
    {
        var stage = ScheduledTripReminderSchedule.ResolveCustomerStage(
            Now.AddMinutes(-1), BookedLongAgo, Now);

        Assert.Null(stage);
    }

    [Fact]
    public void BookedInsideThe30MinuteMark_SkipsThe30MinuteReminder()
    {
        // Booked 20 minutes ahead: the trip is already inside the 30-minute
        // window at creation, so a "30 minutes before" push would arrive
        // seconds after the booking confirmation.
        var scheduledAt = Now.AddMinutes(20);
        var createdAt = Now;

        var stage = ScheduledTripReminderSchedule.ResolveCustomerStage(
            scheduledAt, createdAt, Now);

        Assert.Null(stage);
    }

    [Fact]
    public void BookedInsideThe30MinuteMark_StillSendsThe15MinuteReminder()
    {
        var scheduledAt = Now.AddMinutes(20);
        var createdAt = Now;

        var stage = ScheduledTripReminderSchedule.ResolveCustomerStage(
            scheduledAt, createdAt, Now.AddMinutes(10));

        Assert.Equal(ScheduledTripReminderStage.Customer15Minutes, stage);
    }

    [Fact]
    public void BookedInsideThe15MinuteMark_SendsNoCustomerReminderAtAll()
    {
        var scheduledAt = Now.AddMinutes(12);
        var createdAt = Now;

        var stage = ScheduledTripReminderSchedule.ResolveCustomerStage(
            scheduledAt, createdAt, Now.AddMinutes(1));

        Assert.Null(stage);
    }

    [Fact]
    public void OverduePickup_StillEscalatesToTheAdmin()
    {
        var stages = ScheduledTripReminderSchedule.ResolveDueStages(
            TripStatus.AwaitingAdminAcceptance,
            Now.AddMinutes(-5),
            BookedLongAgo,
            Now).ToList();

        Assert.Equal([ScheduledTripReminderStage.UnacceptedOverdue], stages);
    }
}
