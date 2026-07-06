using Microsoft.Extensions.Logging;
using NSubstitute;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Queries.GetAllTrips;
using Taxi.Application.Features.Trips.Queries.GetPassengerTripCount;
using Taxi.Application.Features.Trips.Queries.GetPassengerTrips;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Drivers;
using Taxi.Domain.Trips;
using Taxi.Domain.Vehicles;
using Xunit;

namespace Taxi.Application.UnitTests.Trips;

public class TripListQueryPaymentVisibilityTests
{
    [Fact]
    public async Task GetPassengerTrips_ExcludesAwaitingPaymentTripsFromItemsAndTotalCount()
    {
        var passengerId = Guid.NewGuid();
        var awaitingQuoteId = Guid.NewGuid();
        var visibleQuoteId = Guid.NewGuid();
        var awaitingTrip = PaymentTestBuilders.CreateAwaitingPaymentTrip(passengerId, awaitingQuoteId);
        var visibleTrip = PaymentTestBuilders.CreateAwaitingPaymentTrip(passengerId, visibleQuoteId);
        visibleTrip.ConfirmPayment();

        var context = BuildContext(
            [awaitingTrip, visibleTrip],
            [
                CreateQuote(passengerId, awaitingTrip.VehicleTypeId, awaitingQuoteId),
                CreateQuote(passengerId, visibleTrip.VehicleTypeId, visibleQuoteId),
            ]);
        var currentUser = Substitute.For<IUser>();
        currentUser.Id.Returns(passengerId.ToString());
        var logger = Substitute.For<ILogger<GetPassengerTripsQueryHandler>>();
        var handler = new GetPassengerTripsQueryHandler(logger, context, currentUser);

        var result = await handler.Handle(new GetPassengerTripsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal(visibleTrip.Id, result.Value.Items[0].Id);
        Assert.DoesNotContain(result.Value.Items, t => t.Status == TripStatus.AwaitingPayment.ToString());
    }

    [Fact]
    public async Task GetPassengerTripCount_ExcludesAwaitingPaymentTrips()
    {
        var passengerId = Guid.NewGuid();
        var awaitingTrip = PaymentTestBuilders.CreateAwaitingPaymentTrip(passengerId, Guid.NewGuid());
        var visibleTrip = PaymentTestBuilders.CreateAwaitingPaymentTrip(passengerId, Guid.NewGuid());
        visibleTrip.ConfirmPayment();

        var context = BuildContext([awaitingTrip, visibleTrip], []);
        var currentUser = Substitute.For<IUser>();
        currentUser.Id.Returns(passengerId.ToString());
        var handler = new GetPassengerTripCountQueryHandler(context, currentUser);

        var result = await handler.Handle(new GetPassengerTripCountQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
    }

    [Fact]
    public async Task GetAllTrips_ExcludesAwaitingPaymentTripsByDefault()
    {
        var passengerId = Guid.NewGuid();
        var awaitingQuoteId = Guid.NewGuid();
        var visibleQuoteId = Guid.NewGuid();
        var awaitingTrip = PaymentTestBuilders.CreateAwaitingPaymentTrip(passengerId, awaitingQuoteId);
        var visibleTrip = PaymentTestBuilders.CreateAwaitingPaymentTrip(passengerId, visibleQuoteId);
        visibleTrip.ConfirmPayment();

        var context = BuildContext(
            [awaitingTrip, visibleTrip],
            [
                CreateQuote(passengerId, awaitingTrip.VehicleTypeId, awaitingQuoteId),
                CreateQuote(passengerId, visibleTrip.VehicleTypeId, visibleQuoteId),
            ]);
        var handler = new GetAllTripsQueryHandler(context, TimeProvider.System);

        var result = await handler.Handle(new GetAllTripsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(visibleTrip.Id, result.Value.Items[0].Id);
        Assert.DoesNotContain(result.Value.Items, t => t.Status == TripStatus.AwaitingPayment.ToString());
    }

    [Fact]
    public async Task GetAllTrips_WhenAwaitingPaymentStatusRequested_ReturnsNoTrips()
    {
        var passengerId = Guid.NewGuid();
        var quoteId = Guid.NewGuid();
        var awaitingTrip = PaymentTestBuilders.CreateAwaitingPaymentTrip(passengerId, quoteId);
        var context = BuildContext(
            [awaitingTrip],
            [CreateQuote(passengerId, awaitingTrip.VehicleTypeId, quoteId)]);
        var handler = new GetAllTripsQueryHandler(context, TimeProvider.System);

        var result = await handler.Handle(
            new GetAllTripsQuery(Status: TripStatus.AwaitingPayment.ToString()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task GetAllTrips_WhenVisibleStatusRequested_ReturnsMatchingTrips()
    {
        var passengerId = Guid.NewGuid();
        var awaitingQuoteId = Guid.NewGuid();
        var pendingQuoteId = Guid.NewGuid();
        var awaitingTrip = PaymentTestBuilders.CreateAwaitingPaymentTrip(passengerId, awaitingQuoteId);
        var pendingTrip = PaymentTestBuilders.CreateAwaitingPaymentTrip(passengerId, pendingQuoteId);
        pendingTrip.ConfirmPayment();

        var context = BuildContext(
            [awaitingTrip, pendingTrip],
            [
                CreateQuote(passengerId, awaitingTrip.VehicleTypeId, awaitingQuoteId),
                CreateQuote(passengerId, pendingTrip.VehicleTypeId, pendingQuoteId),
            ]);
        var handler = new GetAllTripsQueryHandler(context, TimeProvider.System);

        var result = await handler.Handle(
            new GetAllTripsQuery(Status: TripStatus.AwaitingAdminAcceptance.ToString()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(TripStatus.AwaitingAdminAcceptance.ToString(), result.Value.Items[0].Status);
    }

    [Fact]
    public async Task GetAllTrips_WhenPassengerIdProvided_ReturnsOnlyThatPassengersTrips()
    {
        var passengerA = Guid.NewGuid();
        var passengerB = Guid.NewGuid();
        var quoteA = Guid.NewGuid();
        var quoteB = Guid.NewGuid();
        var tripA = PaymentTestBuilders.CreateAwaitingPaymentTrip(passengerA, quoteA);
        var tripB = PaymentTestBuilders.CreateAwaitingPaymentTrip(passengerB, quoteB);
        tripA.ConfirmPayment();
        tripB.ConfirmPayment();

        var context = BuildContext(
            [tripA, tripB],
            [
                CreateQuote(passengerA, tripA.VehicleTypeId, quoteA),
                CreateQuote(passengerB, tripB.VehicleTypeId, quoteB),
            ]);
        var handler = new GetAllTripsQueryHandler(context, TimeProvider.System);

        var result = await handler.Handle(
            new GetAllTripsQuery(PassengerId: passengerA),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(tripA.Id, result.Value.Items[0].Id);
    }

    [Fact]
    public async Task GetAllTrips_ReportsRecordingCountPerTrip()
    {
        var passengerId = Guid.NewGuid();
        var quoteId = Guid.NewGuid();
        var trip = PaymentTestBuilders.CreateAwaitingPaymentTrip(passengerId, quoteId);
        trip.ConfirmPayment();

        var recordings = new List<TripRecording>
        {
            TripRecording.Create(trip.Id, passengerId, RecordingType.Audio, "/recordings/a.m4a", 1000, 30),
            TripRecording.Create(trip.Id, passengerId, RecordingType.Audio, "/recordings/b.m4a", 2000, 45),
            // A different trip's recording must not be counted here.
            TripRecording.Create(Guid.NewGuid(), passengerId, RecordingType.Audio, "/recordings/c.m4a", 3000, 12),
        };

        var context = BuildContext(
            [trip],
            [CreateQuote(passengerId, trip.VehicleTypeId, quoteId)],
            recordings);
        var handler = new GetAllTripsQueryHandler(context, TimeProvider.System);

        var result = await handler.Handle(new GetAllTripsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(2, result.Value.Items[0].RecordingCount);
    }

    [Fact]
    public async Task GetTripRecordings_ReturnsTripRecordingsOrderedAndExcludesDeleted()
    {
        var passengerId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var recordings = new List<TripRecording>
        {
            TripRecording.Create(tripId, passengerId, RecordingType.Audio, "/recordings/a.m4a", 1000, 30),
            TripRecording.Create(Guid.NewGuid(), passengerId, RecordingType.Audio, "/recordings/other.m4a", 2000, 45),
        };

        var context = BuildContext([], [], recordings);
        var handler = new Taxi.Application.Features.Trips.Queries.GetTripRecordings
            .GetTripRecordingsQueryHandler(context);

        var result = await handler.Handle(
            new Taxi.Application.Features.Trips.Queries.GetTripRecordings.GetTripRecordingsQuery(tripId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(tripId, result.Value[0].TripId);
    }

    private static IAppDbContext BuildContext(
        List<Trip> trips,
        List<PricingQuote> pricingQuotes,
        List<TripRecording>? recordings = null)
    {
        var context = Substitute.For<IAppDbContext>();
        var tripsSet = DbSetMockFactory.Create(trips);
        var pricingQuotesSet = DbSetMockFactory.Create(pricingQuotes);
        var vehicleTypesSet = DbSetMockFactory.Create(new List<VehicleType>());
        var driversSet = DbSetMockFactory.Create(new List<Driver>());
        var cancellationsSet = DbSetMockFactory.Create(new List<TripCancellation>());
        var compensationClaimsSet = DbSetMockFactory.Create(new List<TripCompensationClaim>());
        var waitingSessionsSet = DbSetMockFactory.Create(new List<TripWaitingSession>());
        var recordingsSet = DbSetMockFactory.Create(recordings ?? new List<TripRecording>());
        var adminProfilesSet = DbSetMockFactory.Create(
            new List<Taxi.Domain.Admins.AdminProfile>());
        var domainUsersSet = DbSetMockFactory.Create(new List<Taxi.Domain.Users.User>());

        context.DomainUsers.Returns(domainUsersSet);
        context.Trips.Returns(tripsSet);
        context.PricingQuotes.Returns(pricingQuotesSet);
        context.VehicleTypes.Returns(vehicleTypesSet);
        context.Drivers.Returns(driversSet);
        context.TripCancellations.Returns(cancellationsSet);
        context.TripCompensationClaims.Returns(compensationClaimsSet);
        context.TripWaitingSessions.Returns(waitingSessionsSet);
        context.TripRecordings.Returns(recordingsSet);
        context.AdminProfiles.Returns(adminProfilesSet);
        return context;
    }

    private static PricingQuote CreateQuote(Guid passengerId, Guid vehicleTypeId, Guid quoteId)
        => PricingQuote.Create(
            quoteId,
            passengerId,
            vehicleTypeId,
            totalDistanceKm: 5m,
            totalDurationMin: 10m,
            finalFare: 15m,
            originalFare: 15m,
            discountPercent: 0m,
            currencyCode: "eur",
            validUntil: DateTime.UtcNow.AddHours(1),
            stops:
            [
                new Taxi.Domain.Trips.Coordinate(52.37m, 4.89m),
                new Taxi.Domain.Trips.Coordinate(52.38m, 4.90m),
            ]).Value;
}
