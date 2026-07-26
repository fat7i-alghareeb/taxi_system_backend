using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Commands.UpdateTripScheduledTime;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Trips;

using Xunit;

namespace Taxi.Application.UnitTests.Trips;

using TripCoordinate = Taxi.Domain.Trips.Coordinate;

/// <summary>
/// Rescheduling bypasses the preview/apply pipeline, so this handler has to apply
/// <c>TripEditPolicy</c> itself. It previously applied nothing at all and leaned on a stale
/// one-hour guard inside <c>Trip</c> — a policy-only test cannot catch that, which is why the
/// handler is covered separately here.
/// </summary>
public class UpdateTripScheduledTimeCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithinFiveMinutes_UpdatesScheduledTime()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 3);
        var handler = BuildHandler(trip, trip.PassengerId);
        var newPickup = DateTimeOffset.UtcNow.AddHours(2);

        var result = await handler.Handle(
            new UpdateTripScheduledTimeCommand(trip.Id, newPickup), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(newPickup, trip.ScheduledAtUtc);
    }

    [Fact]
    public async Task Handle_AfterFiveMinutes_ReturnsEditWindowExpiredAndLeavesTheTripAlone()
    {
        var originalPickup = DateTimeOffset.UtcNow.AddHours(6);
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 10, scheduledAt: originalPickup);
        var handler = BuildHandler(trip, trip.PassengerId);

        var result = await handler.Handle(
            new UpdateTripScheduledTimeCommand(trip.Id, DateTimeOffset.UtcNow.AddHours(2)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TripErrors.EditWindowExpired.Code, result.Errors[0].Code);
        Assert.Equal(originalPickup, trip.ScheduledAtUtc);
    }

    [Fact]
    public async Task Handle_ScheduledDaysAhead_StillExpiresFiveMinutesAfterBooking()
    {
        // The pickup being far away no longer buys the customer any extra editing time.
        var trip = CreateAcceptedTrip(
            bookedMinutesAgo: 60 * 24,
            scheduledAt: DateTimeOffset.UtcNow.AddDays(3));
        var handler = BuildHandler(trip, trip.PassengerId);

        var result = await handler.Handle(
            new UpdateTripScheduledTimeCommand(trip.Id, DateTimeOffset.UtcNow.AddDays(4)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TripErrors.EditWindowExpired.Code, result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_OnceEnRoute_ReturnsInvalidStatus()
    {
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 1);
        trip.DriverEnRoute(DateTimeOffset.UtcNow);
        var handler = BuildHandler(trip, trip.PassengerId);

        var result = await handler.Handle(
            new UpdateTripScheduledTimeCommand(trip.Id, DateTimeOffset.UtcNow.AddHours(2)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TripErrors.InvalidStatus(TripStatus.EnRoute).Code, result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_NotOwnedByPassenger_FailsOnOwnershipBeforeTheWindow()
    {
        // Ownership must be checked first, so a stranger cannot learn when a trip was booked by
        // reading back EditWindowExpired instead of NotOwnedByPassenger.
        var trip = CreateAcceptedTrip(bookedMinutesAgo: 10);
        var handler = BuildHandler(trip, Guid.NewGuid());

        var result = await handler.Handle(
            new UpdateTripScheduledTimeCommand(trip.Id, DateTimeOffset.UtcNow.AddHours(2)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TripErrors.NotOwnedByPassenger.Code, result.Errors[0].Code);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static UpdateTripScheduledTimeCommandHandler BuildHandler(Trip trip, Guid callerId)
    {
        // Build the set first: NSubstitute rejects configuring a substitute inside Returns().
        var tripsSet = DbSetMockFactory.Create([trip]);

        var context = Substitute.For<IAppDbContext>();
        context.Trips.Returns(tripsSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var currentUser = Substitute.For<IUser>();
        currentUser.Id.Returns(callerId.ToString());
        currentUser.IsAdmin.Returns(false);

        return new UpdateTripScheduledTimeCommandHandler(context, currentUser);
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
            "TRP-RESCHED",
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
