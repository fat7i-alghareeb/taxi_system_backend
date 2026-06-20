using NSubstitute;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Commands.AssignDriverToTrip;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;
using Xunit;

namespace Taxi.Application.UnitTests.Trips;

using TripCoordinate = Taxi.Domain.Trips.Coordinate;

public class AssignDriverToTripCommandHandlerTests
{
    [Fact]
    public async Task Handle_LegacyAssignmentRoute_AcceptsTripForCurrentAdmin()
    {
        var adminId = Guid.NewGuid();
        var scheduledAtUtc = DateTimeOffset.UtcNow.AddHours(3);
        var trip = CreateScheduledTrip(scheduledAtUtc);
        var context = BuildContext(trip);
        var currentUser = Substitute.For<IUser>();
        currentUser.Id.Returns(adminId.ToString());
        currentUser.IsAdmin.Returns(true);
        var handler = new AssignDriverToTripCommandHandler(
            context,
            currentUser,
            TimeProvider.System);

        var result = await handler.Handle(
            new AssignDriverToTripCommand(trip.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.Accepted, trip.Status);
        Assert.Equal(adminId, trip.AcceptedByAdminId);
        Assert.Null(trip.DriverId);
        Assert.Equal(scheduledAtUtc, trip.ScheduledAtUtc);
        await context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Trip CreateScheduledTrip(DateTimeOffset scheduledAtUtc)
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
            "TRP-SCHED",
            passengerId,
            quote,
            stops,
            scheduledAtUtc).Value;
        trip.ConfirmPayment();
        Assert.Equal(TripStatus.AwaitingAdminAcceptance, trip.Status);
        return trip;
    }

    private static IAppDbContext BuildContext(Trip trip)
    {
        var context = Substitute.For<IAppDbContext>();
        var trips = DbSetMockFactory.Create([trip]);
        context.Trips.Returns(trips);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        return context;
    }
}
