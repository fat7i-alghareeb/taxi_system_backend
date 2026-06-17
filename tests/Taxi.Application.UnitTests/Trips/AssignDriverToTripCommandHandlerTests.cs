using NSubstitute;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Commands.AssignDriverToTrip;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Drivers;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;
using Xunit;

namespace Taxi.Application.UnitTests.Trips;

using TripCoordinate = Taxi.Domain.Trips.Coordinate;

public class AssignDriverToTripCommandHandlerTests
{
    [Fact]
    public async Task Handle_FutureScheduledTrip_AssignsDriverAndPreservesScheduledTime()
    {
        var passengerId = Guid.NewGuid();
        var vehicleTypeId = Guid.NewGuid();
        var scheduledAtUtc = DateTimeOffset.UtcNow.AddHours(3);
        var trip = CreateScheduledTrip(passengerId, vehicleTypeId, scheduledAtUtc);
        var driver = CreateApprovedDriver(vehicleTypeId);
        var context = BuildContext(trip, driver);
        var handler = new AssignDriverToTripCommandHandler(context);

        var result = await handler.Handle(
            new AssignDriverToTripCommand(trip.Id, driver.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TripStatus.DriverAssigned, trip.Status);
        Assert.Equal(driver.Id, trip.DriverId);
        Assert.Equal(scheduledAtUtc, trip.ScheduledAtUtc);
        Assert.NotNull(trip.AssignedAtUtc);
        await context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Trip CreateScheduledTrip(
        Guid passengerId,
        Guid vehicleTypeId,
        DateTimeOffset scheduledAtUtc)
    {
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
        Assert.Equal(TripStatus.Scheduled, trip.Status);

        return trip;
    }

    private static Driver CreateApprovedDriver(Guid vehicleTypeId)
    {
        var driver = Driver.Create(Guid.NewGuid(), Guid.NewGuid(), "DL-SCHED-001").Value;
        driver.Approve();
        driver.SetVehicleType(vehicleTypeId);
        return driver;
    }

    private static IAppDbContext BuildContext(Trip trip, Driver driver)
    {
        var tripsSet = DbSetMockFactory.Create([trip]);
        var driversSet = DbSetMockFactory.Create([driver]);
        var context = Substitute.For<IAppDbContext>();
        context.Trips.Returns(tripsSet);
        context.Drivers.Returns(driversSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        return context;
    }
}
