using Taxi.Domain.Trips;
using Xunit;

namespace Taxi.Domain.UnitTests.Trips;

public class TripPaymentTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 6, 20, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConfirmPayment_TransitionsToAwaitingAdminAcceptance(bool scheduled)
    {
        var trip = CreateAwaitingPaymentTrip(
            scheduled ? Now.AddHours(2) : null);

        var result = trip.ConfirmPayment();

        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.AwaitingAdminAcceptance, trip.Status);
        Assert.Null(trip.AcceptedByAdminId);
    }

    [Fact]
    public void ConfirmPayment_WhenAlreadyConfirmed_ReturnsInvalidStatusError()
    {
        var trip = CreateAwaitingPaymentTrip();
        trip.ConfirmPayment();

        var result = trip.ConfirmPayment();

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void AcceptByAdmin_FirstAcceptanceWins()
    {
        var trip = CreateAwaitingPaymentTrip(Now.AddHours(2));
        var firstAdmin = Guid.NewGuid();
        trip.ConfirmPayment();

        var first = trip.AcceptByAdmin(firstAdmin, Now);
        var second = trip.AcceptByAdmin(Guid.NewGuid(), Now.AddMinutes(1));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal(TripErrors.AlreadyAccepted.Code, second.Error.Code);
        Assert.Equal(TripStatus.Accepted, trip.Status);
        Assert.Equal(firstAdmin, trip.AcceptedByAdminId);
        Assert.Equal(Now, trip.AcceptedAtUtc);
        Assert.Null(trip.DriverId);
    }

    [Fact]
    public void AcceptByAdmin_EarlyAcceptanceDoesNotMakeTripEnRoute()
    {
        var trip = CreateAwaitingPaymentTrip(Now.AddHours(2));
        trip.ConfirmPayment();

        trip.AcceptByAdmin(Guid.NewGuid(), Now);

        Assert.Equal(TripStatus.Accepted, trip.Status);
        Assert.False(trip.CanMarkEnRoute(Now));
        Assert.Equal(Now.AddHours(2).AddMinutes(-15), trip.DispatchWindowOpensAtUtc);
    }

    [Fact]
    public void DriverEnRoute_BeforeDispatchWindowIsRejected()
    {
        var trip = CreateAcceptedTrip(Now.AddHours(2));

        var result = trip.DriverEnRoute(Now);

        Assert.True(result.IsFailure);
        Assert.Equal(TripErrors.ScheduledEnRouteNotReady.Code, result.Error.Code);
        Assert.Equal(TripStatus.Accepted, trip.Status);
    }

    [Fact]
    public void DriverEnRoute_AtDispatchWindowTransitionsToEnRoute()
    {
        var scheduledAt = Now.AddHours(2);
        var trip = CreateAcceptedTrip(scheduledAt);

        var result = trip.DriverEnRoute(scheduledAt.AddMinutes(-15));

        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.EnRoute, trip.Status);
    }

    [Fact]
    public void DriverArrived_BeforePickupTimeIsRejected()
    {
        var scheduledAt = Now.AddMinutes(10);
        var trip = CreateAcceptedTrip(scheduledAt);
        trip.DriverEnRoute(Now);

        var result = trip.DriverArrived(Now);

        Assert.True(result.IsFailure);
        Assert.Equal(TripErrors.ScheduledArrivalNotReady.Code, result.Error.Code);
        Assert.Equal(TripStatus.EnRoute, trip.Status);
    }

    [Fact]
    public void CompletedTrip_CanBeRefunded()
    {
        var trip = CreateAcceptedTrip();
        trip.DriverEnRoute(Now);
        trip.DriverArrived(Now);
        trip.Start(Now);
        trip.Complete(Now);

        var result = trip.MarkRefunded(15m);

        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.Refunded, trip.Status);
    }

    [Fact]
    public void ReminderStage_IsRecordedOnceAndRaisesOneEvent()
    {
        var trip = CreateAwaitingPaymentTrip(Now.AddHours(1));
        trip.ConfirmPayment();
        trip.ClearDomainEvents();

        trip.MarkReminderSent(ScheduledTripReminderStage.Unaccepted60Minutes, Now);

        Assert.True(trip.HasReminderBeenSent(ScheduledTripReminderStage.Unaccepted60Minutes));
        Assert.Single(trip.DomainEvents);
    }

    [Fact]
    public void AttentionState_BecomesOverdueWithoutChangingWorkflowStatus()
    {
        var trip = CreateAwaitingPaymentTrip(Now);
        trip.ConfirmPayment();

        var attention = trip.GetAttentionState(Now.AddMinutes(1));

        Assert.Equal(TripAttentionState.Overdue, attention);
        Assert.Equal(TripStatus.AwaitingAdminAcceptance, trip.Status);
    }

    [Fact]
    public void MarkPaymentFailed_WhenAwaitingPayment_TransitionsToPaymentFailed()
    {
        var trip = CreateAwaitingPaymentTrip();

        var result = trip.MarkPaymentFailed("card_declined");

        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.PaymentFailed, trip.Status);
    }

    [Fact]
    public void Cancel_WhenAwaitingPayment_TransitionsToCancelled()
    {
        var trip = CreateAwaitingPaymentTrip();

        var result = trip.Cancel();

        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.Cancelled, trip.Status);
    }

    private static Trip CreateAcceptedTrip(DateTimeOffset? scheduledAt = null)
    {
        var trip = CreateAwaitingPaymentTrip(scheduledAt);
        trip.ConfirmPayment();
        trip.AcceptByAdmin(Guid.NewGuid(), Now);
        return trip;
    }

    private static Trip CreateAwaitingPaymentTrip(DateTimeOffset? scheduledAt = null)
    {
        var passengerId = Guid.NewGuid();
        var vehicleTypeId = Guid.NewGuid();
        var quote = PricingQuote.Create(
            Guid.NewGuid(),
            passengerId,
            vehicleTypeId,
            5m,
            10m,
            15m,
            15m,
            0m,
            "eur",
            DateTime.UtcNow.AddHours(1),
            [new Coordinate(52.37m, 4.89m), new Coordinate(52.38m, 4.90m)]).Value;
        var stops = new[]
        {
            TripStop.Create(new Coordinate(52.37m, 4.89m), 0, "From").Value,
            TripStop.Create(new Coordinate(52.38m, 4.90m), 1, "To").Value,
        };

        return Trip.Request(
            Guid.NewGuid(),
            "TRP-TEST01",
            passengerId,
            quote,
            stops,
            scheduledAt).Value;
    }
}
