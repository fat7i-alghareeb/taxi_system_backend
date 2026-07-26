using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Commands.UpdateTripBagCount;
using Taxi.Application.Features.Trips.Common;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Trips;

using Xunit;

namespace Taxi.Application.UnitTests.Trips;

using TripCoordinate = Taxi.Domain.Trips.Coordinate;

/// <summary>
/// The customer edit window is now a single strict rule: five minutes from booking creation, for
/// route, party size, bags and scheduled time alike. The scheduled-pickup anchor (which used to
/// keep a ride booked days ahead editable until shortly before pickup) was deliberately removed so
/// the customer sees one deadline instead of two. Covers <see cref="TripEditPolicy"/> directly,
/// plus <see cref="UpdateTripBagCountCommandHandler"/> — bags bypass the preview/apply pipeline
/// that <see cref="TripEditPolicy.Validate"/> normally guards, so the handler applies it itself.
/// </summary>
public class TripEditPolicyTests
{
    [Fact]
    public void Validate_StopsWithinWindow_ReturnsNull()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 3);

        var error = TripEditPolicy.Validate(trip, editsStops: true, editsPartySize: false);

        Assert.Null(error);
    }

    [Fact]
    public void Validate_StopsAfterFiveMinutes_ReturnsEditWindowExpired()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 6);

        var error = TripEditPolicy.Validate(trip, editsStops: true, editsPartySize: false);

        Assert.NotNull(error);
        Assert.Equal(TripErrors.EditWindowExpired.Code, error!.Value.Code);
    }

    [Fact]
    public void Validate_PassengersWithinWindow_ReturnsNull()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 3);

        var error = TripEditPolicy.Validate(trip, editsStops: false, editsPartySize: true);

        Assert.Null(error);
    }

    [Fact]
    public void Validate_PassengersAfterFiveMinutes_ReturnsEditWindowExpired()
    {
        // Before this fix, party size had no time gate at all — this is the regression case.
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 6);

        var error = TripEditPolicy.Validate(trip, editsStops: false, editsPartySize: true);

        Assert.NotNull(error);
        Assert.Equal(TripErrors.EditWindowExpired.Code, error!.Value.Code);
    }

    [Fact]
    public void Validate_PassengersOnceEnRoute_ReturnsInvalidStatusEvenWithinWindow()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 1);
        trip.DriverEnRoute(DateTimeOffset.UtcNow);

        var error = TripEditPolicy.Validate(trip, editsStops: false, editsPartySize: true);

        Assert.NotNull(error);
        Assert.Equal(TripErrors.InvalidStatus(TripStatus.EnRoute).Code, error!.Value.Code);
    }

    [Fact]
    public void Validate_ScheduledDaysAhead_StillExpiresFiveMinutesAfterBooking()
    {
        // This used to be allowed: the window was anchored to the pickup, so a ride booked days
        // ahead stayed editable. The rule is now strictly CreatedAtUtc + 5 minutes for every trip.
        var trip = CreateAcceptedTrip(
            bookedMinutesAgo: 60 * 24,
            scheduledAt: DateTimeOffset.UtcNow.AddDays(3));

        var error = TripEditPolicy.Validate(trip, editsStops: false, editsPartySize: true);

        Assert.NotNull(error);
        Assert.Equal(TripErrors.EditWindowExpired.Code, error!.Value.Code);
    }

    [Fact]
    public void Validate_ScheduledSoonAndBookedLongAgo_ReturnsEditWindowExpired()
    {
        var trip = CreateAcceptedTrip(
            bookedMinutesAgo: 60 * 24,
            scheduledAt: DateTimeOffset.UtcNow.AddMinutes(2));

        var error = TripEditPolicy.Validate(trip, editsStops: false, editsPartySize: true);

        Assert.NotNull(error);
        Assert.Equal(TripErrors.EditWindowExpired.Code, error!.Value.Code);
    }

    [Fact]
    public void Validate_ScheduledSoonButJustBooked_StaysEditable()
    {
        // Guards against "simplifying" the rule into a pickup-relative check: a ride booked right
        // now for 20 minutes' time must keep its full five minutes.
        var trip = CreateAcceptedTrip(
            bookedMinutesAgo: 0,
            scheduledAt: DateTimeOffset.UtcNow.AddMinutes(20));

        var error = TripEditPolicy.Validate(trip, editsStops: true, editsPartySize: true);

        Assert.Null(error);
    }

    [Fact]
    public void Validate_ScheduleWithinWindow_ReturnsNull()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 3);

        var error = TripEditPolicy.Validate(
            trip, editsStops: false, editsPartySize: false, editsSchedule: true);

        Assert.Null(error);
    }

    [Fact]
    public void Validate_ScheduleAfterFiveMinutes_ReturnsEditWindowExpired()
    {
        // Before this fix the scheduled-time endpoint had no policy check at all and relied on a
        // stale one-hour domain guard — this is the regression case.
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 6);

        var error = TripEditPolicy.Validate(
            trip, editsStops: false, editsPartySize: false, editsSchedule: true);

        Assert.NotNull(error);
        Assert.Equal(TripErrors.EditWindowExpired.Code, error!.Value.Code);
    }

    [Fact]
    public void Validate_ScheduleOnceEnRoute_ReturnsInvalidStatusEvenWithinWindow()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 1);
        trip.DriverEnRoute(DateTimeOffset.UtcNow);

        var error = TripEditPolicy.Validate(
            trip, editsStops: false, editsPartySize: false, editsSchedule: true);

        Assert.NotNull(error);
        Assert.Equal(TripErrors.InvalidStatus(TripStatus.EnRoute).Code, error!.Value.Code);
    }

    [Fact]
    public async Task BagCountHandler_WithinFiveMinutes_Succeeds()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 3);
        var handler = BuildHandler(trip, out _);

        var result = await handler.Handle(
            new UpdateTripBagCountCommand(trip.Id, 4), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, trip.BagCount);
    }

    [Fact]
    public async Task BagCountHandler_AfterFiveMinutes_ReturnsEditWindowExpired()
    {
        // Bags used to be status-only (no window at all) — this is the regression case.
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 10);
        var handler = BuildHandler(trip, out var notifier);

        var result = await handler.Handle(
            new UpdateTripBagCountCommand(trip.Id, 4), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TripErrors.EditWindowExpired.Code, result.Errors[0].Code);
        Assert.Equal(0, trip.BagCount);
        await notifier.DidNotReceiveWithAnyArgs().NotifyAsync(default!, default);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static UpdateTripBagCountCommandHandler BuildHandler(
        Trip trip, out ITripAdminEditNotifier notifier)
    {
        // Build the set first: NSubstitute rejects configuring a substitute inside Returns().
        var tripsSet = DbSetMockFactory.Create([trip]);

        var context = Substitute.For<IAppDbContext>();
        context.Trips.Returns(tripsSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var currentUser = Substitute.For<IUser>();
        currentUser.Id.Returns(trip.PassengerId.ToString());
        currentUser.IsAdmin.Returns(false);

        notifier = Substitute.For<ITripAdminEditNotifier>();

        return new UpdateTripBagCountCommandHandler(context, currentUser, notifier);
    }

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
            [new TripCoordinate(52.37m, 4.89m), new TripCoordinate(52.38m, 4.90m)]).Value;

        var stops = new[]
        {
            TripStop.Create(new TripCoordinate(52.37m, 4.89m), 0, "From").Value,
            TripStop.Create(new TripCoordinate(52.38m, 4.90m), 1, "To").Value,
        };

        var trip = Trip.Request(
            Guid.NewGuid(),
            "TRP-EDITPOLICY",
            passengerId,
            quote,
            stops,
            scheduledAt).Value;

        // Normally stamped by the auditing interceptor on save.
        trip.CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-bookedMinutesAgo);

        trip.ConfirmPayment();
        trip.AcceptByAdmin(Guid.NewGuid(), DateTimeOffset.UtcNow);

        return trip;
    }
}
