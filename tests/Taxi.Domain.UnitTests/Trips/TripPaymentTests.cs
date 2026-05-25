using Taxi.Domain.Trips;
using Xunit;

namespace Taxi.Domain.UnitTests.Trips;

public class TripPaymentTests
{
    // ── ConfirmPayment ───────────────────────────────────────────────────────

    [Fact]
    public void ConfirmPayment_WhenAwaitingPayment_TransitionsToPendingDriver()
    {
        var trip = CreateAwaitingPaymentTrip();

        var result = trip.ConfirmPayment();

        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.PendingDriver, trip.Status);
    }

    [Fact]
    public void ConfirmPayment_WhenAwaitingPaymentAndScheduled_TransitionsToScheduled()
    {
        var trip = CreateAwaitingPaymentTrip(scheduledAt: DateTimeOffset.UtcNow.AddHours(2));

        var result = trip.ConfirmPayment();

        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.Scheduled, trip.Status);
    }

    [Fact]
    public void ConfirmPayment_WhenAlreadyPendingDriver_ReturnsInvalidStatusError()
    {
        var trip = CreateAwaitingPaymentTrip();
        trip.ConfirmPayment();

        var result = trip.ConfirmPayment();

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ConfirmPayment_WhenPaymentFailed_ReturnsInvalidStatusError()
    {
        var trip = CreateAwaitingPaymentTrip();
        trip.MarkPaymentFailed("card_declined");

        var result = trip.ConfirmPayment();

        Assert.True(result.IsFailure);
    }

    // ── MarkPaymentFailed ────────────────────────────────────────────────────

    [Fact]
    public void MarkPaymentFailed_WhenAwaitingPayment_TransitionsToPaymentFailed()
    {
        var trip = CreateAwaitingPaymentTrip();

        var result = trip.MarkPaymentFailed("card_declined");

        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.PaymentFailed, trip.Status);
    }

    [Fact]
    public void MarkPaymentFailed_WhenNotAwaitingPayment_ReturnsInvalidStatusError()
    {
        var trip = CreateAwaitingPaymentTrip();
        trip.ConfirmPayment();

        var result = trip.MarkPaymentFailed("card_declined");

        Assert.True(result.IsFailure);
    }

    // ── MarkRefunded ─────────────────────────────────────────────────────────

    [Fact]
    public void MarkRefunded_WhenCancelled_TransitionsToRefunded()
    {
        var trip = CreateAwaitingPaymentTrip();
        trip.Cancel();

        var result = trip.MarkRefunded(15m);

        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.Refunded, trip.Status);
    }

    [Fact]
    public void MarkRefunded_WhenCompleted_TransitionsToRefunded()
    {
        var trip = CreateAwaitingPaymentTrip();
        trip.ConfirmPayment();
        trip.AssignDriver(Guid.NewGuid());
        trip.DriverEnRoute();
        trip.DriverArrived();
        trip.Start();
        trip.Complete();

        var result = trip.MarkRefunded(15m);

        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.Refunded, trip.Status);
    }

    [Fact]
    public void MarkRefunded_WhenInProgress_ReturnsInvalidStatusError()
    {
        var trip = CreateAwaitingPaymentTrip();
        trip.ConfirmPayment();
        trip.AssignDriver(Guid.NewGuid());
        trip.DriverEnRoute();
        trip.DriverArrived();
        trip.Start();

        var result = trip.MarkRefunded(15m);

        Assert.True(result.IsFailure);
    }

    // ── Cancel ───────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_WhenAwaitingPayment_TransitionsToCancelled()
    {
        var trip = CreateAwaitingPaymentTrip();

        var result = trip.Cancel();

        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.Cancelled, trip.Status);
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

        return Trip.Request(Guid.NewGuid(), "TRP-TEST01", passengerId, quote, stops, scheduledAt).Value;
    }
}
