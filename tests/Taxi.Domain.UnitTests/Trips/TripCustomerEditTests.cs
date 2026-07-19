using Taxi.Domain.Trips;
using Xunit;

namespace Taxi.Domain.UnitTests.Trips;

/// <summary>
/// The customer edit rules deliberately differ per field: the route is bounded by a time window
/// but stays editable through arrival, while party size has no window and closes the moment the
/// driver starts moving (a passenger-count change can swap the assigned vehicle).
/// </summary>
public class TripCustomerEditTests
{
    [Fact]
    public void UpdateStops_WithinWindow_Succeeds()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 10);

        var result = trip.UpdateStops(BuildStops(52.40m, 4.95m));

        Assert.True(result.IsSuccess);
        Assert.Equal(52.40m, trip.DropoffStop!.Coordinate.Latitude);
    }

    [Fact]
    public void UpdateStops_AfterWindowClosed_ReturnsEditWindowExpired()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 90);

        var result = trip.UpdateStops(BuildStops(52.40m, 4.95m));

        Assert.True(result.IsError);
        Assert.Equal(TripErrors.EditWindowExpired.Code, result.Errors[0].Code);
    }

    [Fact]
    public void UpdateStops_ScheduledDaysAhead_StaysEditableLongAfterBooking()
    {
        // The booking hour is long gone, but the ride is days away — the customer must still be
        // able to correct the address. This is the case a CreatedAtUtc-only window got wrong.
        var trip = CreateAcceptedTrip(
            bookedMinutesAgo: 60 * 24,
            scheduledAt: DateTimeOffset.UtcNow.AddDays(3));

        var result = trip.UpdateStops(BuildStops(52.40m, 4.95m));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void UpdateStops_ScheduledWithinTheHour_ClosesOnceThePickupIsNear()
    {
        var trip = CreateAcceptedTrip(
            bookedMinutesAgo: 60 * 24,
            scheduledAt: DateTimeOffset.UtcNow.AddMinutes(30));

        var result = trip.UpdateStops(BuildStops(52.40m, 4.95m));

        Assert.True(result.IsError);
        Assert.Equal(TripErrors.EditWindowExpired.Code, result.Errors[0].Code);
    }

    [Fact]
    public void UpdateStops_ScheduledSoonButJustBooked_KeepsTheFullBookingHour()
    {
        // Without taking the later of the two deadlines, a ride booked for 20 minutes' time would
        // be locked the instant it was created.
        var trip = CreateAcceptedTrip(
            bookedMinutesAgo: 0,
            scheduledAt: DateTimeOffset.UtcNow.AddMinutes(20));

        var result = trip.UpdateStops(BuildStops(52.40m, 4.95m));

        Assert.True(result.IsSuccess);
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
