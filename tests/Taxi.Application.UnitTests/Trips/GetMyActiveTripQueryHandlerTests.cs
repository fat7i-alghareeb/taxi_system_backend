using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Queries.GetMyActiveTrip;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Drivers;
using Taxi.Domain.Trips;

using Xunit;

namespace Taxi.Application.UnitTests.Trips;

public class GetMyActiveTripQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserHasNoActiveTrip_ReturnsSuccessfulNull()
    {
        var userId = Guid.NewGuid();
        var context = Substitute.For<IAppDbContext>();
        var trips = DbSetMockFactory.Create(new List<Trip>());
        var drivers = DbSetMockFactory.Create(new List<Driver>());
        context.Trips.Returns(trips);
        context.Drivers.Returns(drivers);

        var currentUser = Substitute.For<IUser>();
        currentUser.Id.Returns(userId.ToString());

        var handler = new GetMyActiveTripQueryHandler(
            context,
            currentUser,
            TimeProvider.System);

        var result = await handler.Handle(
            new GetMyActiveTripQuery(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }
}
