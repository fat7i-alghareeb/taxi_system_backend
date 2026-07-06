using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Commands.StartTrip;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Trips;

using Xunit;

namespace Taxi.Application.UnitTests.Trips;

using TripCoordinate = Taxi.Domain.Trips.Coordinate;

public class StartTripCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenActiveWaitingSessionExists_StopsItAtTripStartTime()
    {
        var adminId = Guid.NewGuid();
        var waitingStartedAt = new DateTimeOffset(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);
        var tripStartAt = waitingStartedAt.AddMinutes(10);
        var trip = CreateArrivedTrip(adminId, tripStartAt);
        var waitingSession = TripWaitingSession.Start(
            Guid.NewGuid(),
            trip.Id,
            adminId,
            0.13m,
            TripWaitingSession.DefaultGraceMinutes,
            waitingStartedAt).Value;
        var context = BuildContext(trip, waitingSession);
        var currentUser = Substitute.For<IUser>();
        currentUser.Id.Returns(adminId.ToString());
        currentUser.IsAdmin.Returns(true);
        var handler = new StartTripCommandHandler(
            context,
            currentUser,
            new FakeTimeProvider(tripStartAt));

        var result = await handler.Handle(new StartTripCommand(trip.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.InProgress, trip.Status);
        Assert.False(waitingSession.IsActive);
        Assert.Equal(tripStartAt, waitingSession.StoppedAtUtc);
        Assert.Equal(10, waitingSession.Minutes);
        Assert.Equal(0, waitingSession.BillableMinutes);
        Assert.Equal(0m, waitingSession.EstimatedFee);
        await context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Trip CreateArrivedTrip(Guid adminId, DateTimeOffset now)
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
            "TRP-START",
            passengerId,
            quote,
            stops).Value;
        trip.ConfirmPayment();
        trip.AcceptByAdmin(adminId, now);
        trip.DriverEnRoute(now, forceOverride: true);
        trip.DriverArrived(now);
        return trip;
    }

    private static IAppDbContext BuildContext(Trip trip, TripWaitingSession waitingSession)
    {
        var context = Substitute.For<IAppDbContext>();
        var trips = DbSetMockFactory.Create([trip]);
        var waitingSessions = DbSetMockFactory.Create([waitingSession]);
        context.Trips.Returns(trips);
        context.TripWaitingSessions.Returns(waitingSessions);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        return context;
    }

    private sealed class FakeTimeProvider(DateTimeOffset fixedNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => fixedNow;
    }
}
