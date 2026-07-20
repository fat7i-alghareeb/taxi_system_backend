using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Common;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Trips;
using Taxi.Domain.Vehicles;
using Xunit;

namespace Taxi.Application.UnitTests.Trips;

using TripCoordinate = Taxi.Domain.Trips.Coordinate;

/// <summary>
/// Covers <see cref="TripRequoteService.ResolveFromPreviewAsync"/> — the path that makes an edit
/// cost what the customer was told it would cost. Re-pricing live on apply meant the directions
/// provider could return a slightly different distance seconds after the preview, moving the delta
/// and tripping the drift guard, so the edit failed for no reason the customer could act on.
/// </summary>
public class TripRequoteServicePreviewTokenTests
{
    private static readonly Guid _passengerId = Guid.NewGuid();
    private const string Currency = "eur";

    private static readonly TripCoordinate _pickup = new(52.37m, 4.89m);
    private static readonly TripCoordinate _oldDropoff = new(52.38m, 4.90m);
    private static readonly TripCoordinate _newDropoff = new(52.42m, 4.95m);

    [Fact]
    public async Task ResolveFromPreview_ReusesQuotedFare_WithoutCallingDirections()
    {
        var (trip, currentQuote, vehicle) = BuildTrip(fare: 20m, capacity: 4);
        var previewQuote = BuildPreviewQuote(vehicle.Id, fare: 32.50m, [_pickup, _newDropoff]);
        var directions = Substitute.For<IDirectionsService>();
        var service = BuildService(trip, [currentQuote, previewQuote], [vehicle], directions);

        var result = await service.ResolveFromPreviewAsync(
            trip, previewQuote.Id, NewStops(), newPassengerCount: null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(32.50m, result.Value.NewFinalFare);
        Assert.Equal(20m, result.Value.OldFinalFare);
        Assert.Equal(12.50m, result.Value.Delta);
        Assert.Same(previewQuote, result.Value.NewQuote);

        // The whole point: no second routing call, so no jitter and no drift.
        await directions.DidNotReceiveWithAnyArgs().GetDirectionsAsync(default!);
    }

    [Fact]
    public async Task ResolveFromPreview_AlreadyUsedQuote_ReturnsDeltaChanged()
    {
        var (trip, currentQuote, vehicle) = BuildTrip(fare: 20m, capacity: 4);
        var previewQuote = BuildPreviewQuote(vehicle.Id, fare: 32.50m, [_pickup, _newDropoff]);
        previewQuote.MarkAsUsed();
        var service = BuildService(trip, [currentQuote, previewQuote], [vehicle]);

        var result = await service.ResolveFromPreviewAsync(
            trip, previewQuote.Id, NewStops(), newPassengerCount: null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TripErrors.EditDeltaChanged.Code, result.Error.Code);
    }

    [Fact]
    public async Task ResolveFromPreview_ExpiredQuote_ReturnsDeltaChanged()
    {
        var (trip, currentQuote, vehicle) = BuildTrip(fare: 20m, capacity: 4);
        var previewQuote = BuildPreviewQuote(
            vehicle.Id, fare: 32.50m, [_pickup, _newDropoff], validUntil: DateTime.UtcNow.AddMinutes(-1));
        var service = BuildService(trip, [currentQuote, previewQuote], [vehicle]);

        var result = await service.ResolveFromPreviewAsync(
            trip, previewQuote.Id, NewStops(), newPassengerCount: null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TripErrors.EditDeltaChanged.Code, result.Error.Code);
    }

    [Fact]
    public async Task ResolveFromPreview_StopsDoNotMatchQuote_ReturnsDeltaChanged()
    {
        // Priced for a short hop, then applied against a far-away drop-off — a cheap preview
        // must not be replayable against an expensive change.
        var (trip, currentQuote, vehicle) = BuildTrip(fare: 20m, capacity: 4);
        var previewQuote = BuildPreviewQuote(vehicle.Id, fare: 21m, [_pickup, _oldDropoff]);
        var service = BuildService(trip, [currentQuote, previewQuote], [vehicle]);

        var result = await service.ResolveFromPreviewAsync(
            trip, previewQuote.Id, NewStops(), newPassengerCount: null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TripErrors.EditDeltaChanged.Code, result.Error.Code);
    }

    [Fact]
    public async Task ResolveFromPreview_QuotedVehicleTooSmallForParty_ReturnsDeltaChanged()
    {
        // Preview 2 passengers (sedan price), apply 6 — the sedan quote must not stand.
        var (trip, currentQuote, sedan) = BuildTrip(fare: 20m, capacity: 4);
        var previewQuote = BuildPreviewQuote(sedan.Id, fare: 21m, [_pickup, _newDropoff]);
        var service = BuildService(trip, [currentQuote, previewQuote], [sedan]);

        var result = await service.ResolveFromPreviewAsync(
            trip, previewQuote.Id, NewStops(), newPassengerCount: 6, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TripErrors.EditDeltaChanged.Code, result.Error.Code);
    }

    [Fact]
    public async Task ResolveFromPreview_VanUpgrade_ReportsTheNewVehicle()
    {
        var (trip, currentQuote, sedan) = BuildTrip(fare: 20m, capacity: 4);
        var van = BuildVehicle("VAN", capacity: 8);
        var previewQuote = BuildPreviewQuote(van.Id, fare: 41m, [_pickup, _oldDropoff]);
        var service = BuildService(trip, [currentQuote, previewQuote], [sedan, van]);

        var result = await service.ResolveFromPreviewAsync(
            trip, previewQuote.Id, newStops: null, newPassengerCount: 6, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(van.Id, result.Value.NewVehicleTypeId);
        Assert.Equal(21m, result.Value.Delta);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static List<TripEditStop> NewStops() =>
    [
        new(_pickup.Latitude, _pickup.Longitude, "From"),
        new(_newDropoff.Latitude, _newDropoff.Longitude, "To"),
    ];

    private static TripRequoteService BuildService(
        Trip trip,
        List<PricingQuote> quotes,
        List<VehicleType> vehicles,
        IDirectionsService? directions = null)
    {
        // Build every set first: NSubstitute rejects configuring a substitute inside Returns().
        var tripsSet = DbSetMockFactory.Create([trip]);
        var quotesSet = DbSetMockFactory.Create(quotes);
        var vehicleTypesSet = DbSetMockFactory.Create(vehicles);
        var directionsService = directions ?? Substitute.For<IDirectionsService>();

        var context = Substitute.For<IAppDbContext>();
        context.Trips.Returns(tripsSet);
        context.PricingQuotes.Returns(quotesSet);
        context.VehicleTypes.Returns(vehicleTypesSet);
        return new TripRequoteService(context, directionsService);
    }

    private static (Trip trip, PricingQuote quote, VehicleType vehicle) BuildTrip(
        decimal fare,
        int capacity)
    {
        var vehicle = BuildVehicle("SEDAN", capacity);
        var quote = PricingQuote.Create(
            Guid.NewGuid(), _passengerId, vehicle.Id,
            5m, 10m, fare, fare, 0m, Currency,
            DateTime.UtcNow.AddHours(2),
            [_pickup, _oldDropoff]).Value;

        var stops = new[]
        {
            TripStop.Create(_pickup, 0, "From").Value,
            TripStop.Create(_oldDropoff, 1, "To").Value,
        };

        var trip = Trip.Request(
            Guid.NewGuid(), "TRP-REQUOTE-TEST", _passengerId, quote, stops, scheduledAtUtc: null).Value;

        return (trip, quote, vehicle);
    }

    private static PricingQuote BuildPreviewQuote(
        Guid vehicleTypeId,
        decimal fare,
        IEnumerable<TripCoordinate> stops,
        DateTime? validUntil = null) =>
        PricingQuote.Create(
            Guid.NewGuid(), _passengerId, vehicleTypeId,
            8m, 16m, fare, fare, 0m, Currency,
            validUntil ?? DateTime.UtcNow.AddMinutes(15),
            stops).Value;

    private static VehicleType BuildVehicle(string code, int capacity) =>
        VehicleType.Create(
            Guid.NewGuid(), code,
            code, code, code, code, code, code, code, code, code,
            capacity, 1.5m, 0.4m, 5m).Value;
}
