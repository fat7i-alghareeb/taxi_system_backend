using Taxi.Domain.Trips;
using Xunit;

namespace Taxi.Domain.UnitTests.Trips;

/// <summary>
/// The domain gates customer edits on status and shape only — there is deliberately no clock here.
/// Every edit deadline (the five-minute customer window) lives in <c>TripEditPolicy</c> in the
/// Application layer, because these same mutation methods are reached from the async Stripe-webhook
/// commit path: a window check here would discard an edit the rider has already paid for. Do not
/// re-add a time window to <c>Trip</c>. The window itself is covered in
/// <c>Taxi.Application.UnitTests.Trips.TripEditPolicyTests</c>.
///
/// The status gates still differ per field: the route stays editable through arrival, while party
/// size closes the moment the driver starts moving (a passenger-count change can swap the assigned
/// vehicle).
/// </summary>
public class TripCustomerEditTests
{
    [Fact]
    public void UpdateStops_WhileAccepted_Succeeds()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 10);

        var result = trip.UpdateStops(BuildStops(52.40m, 4.95m));

        Assert.True(result.IsSuccess);
        Assert.Equal(52.40m, trip.DropoffStop!.Coordinate.Latitude);
    }

    [Fact]
    public void UpdateStops_LongAfterBooking_StillSucceedsBecauseTheWindowLivesInThePolicy()
    {
        // Regression test for the webhook-commit case: a rider previews an edit at t+4:30, pays for
        // it, and Stripe's confirmation lands at t+5:30. TripEditApplier calls straight into
        // UpdateStops at that point, so a clock guard here would silently drop a paid-for re-route.
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 90);

        var result = trip.UpdateStops(BuildStops(52.40m, 4.95m));

        Assert.True(result.IsSuccess);
        Assert.Equal(52.40m, trip.DropoffStop!.Coordinate.Latitude);
    }

    [Fact]
    public void UpdateScheduledTime_LongAfterBooking_Succeeds()
    {
        // Same reasoning as UpdateStops: the five-minute rule is enforced by the handler.
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 90);
        var newPickup = DateTimeOffset.UtcNow.AddHours(4);

        var result = trip.UpdateScheduledTime(newPickup);

        Assert.True(result.IsSuccess);
        Assert.Equal(newPickup, trip.ScheduledAtUtc);
    }

    [Fact]
    public void UpdateScheduledTime_OnceEnRoute_ReturnsInvalidStatus()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 1);
        trip.DriverEnRoute(DateTimeOffset.UtcNow);

        var result = trip.UpdateScheduledTime(DateTimeOffset.UtcNow.AddHours(4));

        Assert.True(result.IsError);
        Assert.Equal(TripErrors.InvalidStatus(TripStatus.EnRoute).Code, result.Errors[0].Code);
    }

    [Fact]
    public void UpdateStops_WhileInProgress_ReturnsInvalidStatus()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 5);
        var now = DateTimeOffset.UtcNow;
        trip.DriverEnRoute(now);
        trip.DriverArrived(now);
        trip.Start(now);

        var result = trip.UpdateStops(BuildStops(52.40m, 4.95m));

        Assert.True(result.IsError);
        Assert.Equal(TripErrors.InvalidStatus(TripStatus.InProgress).Code, result.Errors[0].Code);
    }

    [Fact]
    public void UpdatePassengerCount_BeforeDriverMoves_Succeeds()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 5);

        var result = trip.UpdatePassengerCount(3);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, trip.PassengerCount);
    }

    [Fact]
    public void UpdatePassengerCount_OnceEnRoute_ReturnsInvalidStatus()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 5);
        trip.DriverEnRoute(DateTimeOffset.UtcNow);

        var result = trip.UpdatePassengerCount(3);

        Assert.True(result.IsError);
        Assert.Equal(TripErrors.InvalidStatus(TripStatus.EnRoute).Code, result.Errors[0].Code);
        Assert.Equal(1, trip.PassengerCount);
    }

    [Fact]
    public void UpdateBagCount_LongAfterBooking_Succeeds()
    {
        // Bags used to carry a one-hour window. Luggage does not affect routing or vehicle choice,
        // so status alone gates it now.
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 120);

        var result = trip.UpdateBagCount(4);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, trip.BagCount);
    }

    [Fact]
    public void UpdateBagCount_OnceEnRoute_ReturnsInvalidStatus()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 5);
        trip.DriverEnRoute(DateTimeOffset.UtcNow);

        var result = trip.UpdateBagCount(4);

        Assert.True(result.IsError);
        Assert.Equal(TripErrors.InvalidStatus(TripStatus.EnRoute).Code, result.Errors[0].Code);
    }

    [Fact]
    public void Rate_SecondAttempt_ReturnsAlreadyRatedAndKeepsTheOriginal()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 5);
        var now = DateTimeOffset.UtcNow;
        trip.DriverEnRoute(now);
        trip.DriverArrived(now);
        trip.Start(now);
        trip.Complete(now);

        var first = trip.Rate(5, "Great");
        var second = trip.Rate(1, "Changed my mind");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsError);
        Assert.Equal(TripErrors.AlreadyRated.Code, second.Errors[0].Code);
        Assert.Equal(5, trip.PassengerRating);
        Assert.Equal("Great", trip.RatingComment);
    }

    private static IReadOnlyList<TripStop> BuildStops(decimal dropoffLat, decimal dropoffLng) =>
    [
        TripStop.Create(new Coordinate(52.37m, 4.89m), 0, "From").Value,
        TripStop.Create(new Coordinate(dropoffLat, dropoffLng), 1, "To").Value,
    ];

    private static Trip CreateAcceptedTrip(int bookedMinutesAgo, DateTimeOffset? scheduledAt = null)
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

        var trip = Trip.Request(
            Guid.NewGuid(),
            "TRP-EDIT01",
            passengerId,
            quote,
            BuildStops(52.38m, 4.90m),
            scheduledAt).Value;

        // Normally stamped by the auditing interceptor on save.
        trip.CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-bookedMinutesAgo);

        trip.ConfirmPayment();
        trip.AcceptByAdmin(Guid.NewGuid(), DateTimeOffset.UtcNow);

        return trip;
    }
}
