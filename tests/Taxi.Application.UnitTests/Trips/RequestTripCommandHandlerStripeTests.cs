using NSubstitute;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Commands.RequestTrip;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;
using Taxi.Domain.Vehicles;
using Xunit;

namespace Taxi.Application.UnitTests.Trips;

public class RequestTripCommandHandlerStripeTests
{
    private readonly Guid _passengerId = Guid.NewGuid();
    private readonly Guid _vehicleTypeId = Guid.NewGuid();
    private readonly IUser _user = Substitute.For<IUser>();
    private readonly IClientConfigProvider _clientConfig = Substitute.For<IClientConfigProvider>();
    private readonly IStripePaymentService _stripe = Substitute.For<IStripePaymentService>();

    public RequestTripCommandHandlerStripeTests()
    {
        _user.Id.Returns(_passengerId.ToString());
    }

    // ── Stripe disabled (legacy path) ────────────────────────────────────────

    [Fact]
    public async Task Handle_StripeDisabled_ReturnsTripWithoutStripePayment()
    {
        _clientConfig.GetClientConfig().Returns(new ClientConfig(StripeEnabled: false, StripePublishableKey: string.Empty, SignalREnabled: true));
        var (context, quoteId) = BuildContext(_passengerId, _vehicleTypeId);
        var handler = new RequestTripCommandHandler(context, _user, _clientConfig, _stripe, TimeProvider.System);

        var result = await handler.Handle(new RequestTripCommand(quoteId, TwoStops()), default);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.StripePayment);
    }

    [Fact]
    public async Task Handle_StripeDisabled_TripStatusIsAwaitingAdminAcceptance()
    {
        _clientConfig.GetClientConfig().Returns(new ClientConfig(StripeEnabled: false, StripePublishableKey: string.Empty, SignalREnabled: true));
        var (context, quoteId) = BuildContext(_passengerId, _vehicleTypeId);
        var handler = new RequestTripCommandHandler(context, _user, _clientConfig, _stripe, TimeProvider.System);

        var result = await handler.Handle(new RequestTripCommand(quoteId, TwoStops()), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("AwaitingAdminAcceptance", result.Value.Status);
    }

    [Fact]
    public async Task Handle_AirportPickup_NormalizesAndReturnsFlightNumber()
    {
        _clientConfig.GetClientConfig().Returns(new ClientConfig(StripeEnabled: false, StripePublishableKey: string.Empty, SignalREnabled: true));
        var (context, quoteId) = BuildContext(_passengerId, _vehicleTypeId);
        var handler = new RequestTripCommandHandler(
            context,
            _user,
            _clientConfig,
            _stripe,
            TimeProvider.System);
        var stops = TwoStops();
        stops[0] = stops[0] with { IsAirport = true };

        var result = await handler.Handle(
            new RequestTripCommand(quoteId, stops, FlightNumber: " tk   1864 "),
            default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsAirport);
        Assert.Equal("TK 1864", result.Value.FlightNumber);
    }

    // ── Stripe enabled (payment path) ────────────────────────────────────────

    [Fact]
    public async Task Handle_StripeEnabled_ReturnsTripWithClientSecret()
    {
        _clientConfig.GetClientConfig().Returns(new ClientConfig(StripeEnabled: true, StripePublishableKey: "pk_test", SignalREnabled: true));
        _stripe.CreatePaymentIntentAsync(
                Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<Guid>(), Arg.Any<Guid>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult("pi_test_123", "cs_test_secret", "pk_test", "cus_test_123", "ek_test_secret"));
        var (context, quoteId) = BuildContext(_passengerId, _vehicleTypeId);
        var handler = new RequestTripCommandHandler(context, _user, _clientConfig, _stripe, TimeProvider.System);

        var result = await handler.Handle(new RequestTripCommand(quoteId, TwoStops()), default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.StripePayment);
        Assert.Equal("cs_test_secret", result.Value.StripePayment!.ClientSecret);
        Assert.Equal("pi_test_123", result.Value.StripePayment.PaymentIntentId);
    }

    [Fact]
    public async Task Handle_StripeEnabled_PersistsPaymentRecord()
    {
        _clientConfig.GetClientConfig().Returns(new ClientConfig(StripeEnabled: true, StripePublishableKey: "pk_test", SignalREnabled: true));
        _stripe.CreatePaymentIntentAsync(
                Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<Guid>(), Arg.Any<Guid>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult("pi_test_123", "cs_test_secret", "pk_test", "cus_test_123", "ek_test_secret"));
        var (context, quoteId) = BuildContext(_passengerId, _vehicleTypeId);
        var handler = new RequestTripCommandHandler(context, _user, _clientConfig, _stripe, TimeProvider.System);

        await handler.Handle(new RequestTripCommand(quoteId, TwoStops()), default);

        context.Payments.Received(1).Add(Arg.Is<Payment>(p => p.StripePaymentIntentId == "pi_test_123"));
    }

    // ── Error paths ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_StripeEnabled_StripeApiFails_ReturnsStripeInitiationFailedError()
    {
        _clientConfig.GetClientConfig().Returns(new ClientConfig(StripeEnabled: true, StripePublishableKey: "pk_test", SignalREnabled: true));
        _stripe.CreatePaymentIntentAsync(
                Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<Guid>(), Arg.Any<Guid>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(PaymentErrors.StripeInitiationFailed);
        var (context, quoteId) = BuildContext(_passengerId, _vehicleTypeId);
        var handler = new RequestTripCommandHandler(context, _user, _clientConfig, _stripe, TimeProvider.System);

        var result = await handler.Handle(new RequestTripCommand(quoteId, TwoStops()), default);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.StripeInitiationFailed.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_QuoteAlreadyUsed_ReturnsQuoteAlreadyUsedError()
    {
        _clientConfig.GetClientConfig().Returns(new ClientConfig(StripeEnabled: false, StripePublishableKey: string.Empty, SignalREnabled: true));
        var (context, quoteId) = BuildContext(_passengerId, _vehicleTypeId, quoteUsed: true);
        var handler = new RequestTripCommandHandler(context, _user, _clientConfig, _stripe, TimeProvider.System);

        var result = await handler.Handle(new RequestTripCommand(quoteId, TwoStops()), default);

        Assert.True(result.IsFailure);
        Assert.Equal(TripErrors.QuoteAlreadyUsed.Code, result.Error.Code);
    }

    private (IAppDbContext Context, Guid QuoteId) BuildContext(Guid passengerId, Guid vehicleTypeId, bool quoteUsed = false)
    {
        var quoteId = Guid.NewGuid();
        var quote = PricingQuote.Create(
            quoteId,
            passengerId,
            vehicleTypeId,
            5m,
            10m,
            15m,
            15m,
            0m,
            "eur",
            DateTime.UtcNow.AddHours(1),
            [new(52.37m, 4.89m), new(52.38m, 4.90m)]).Value;
        if (quoteUsed)
        {
            quote.MarkAsUsed();
        }

        var passenger = PaymentTestBuilders.CreatePassenger(passengerId);
        var driver = PaymentTestBuilders.CreateActiveDriver();
        var vehicleType = VehicleType.Create(
            vehicleTypeId,
            "standard", "Standard", "عادي", "Standaard",
            "Standard", "Standard", "Standard", "Standard", "Standard", "Standard",
            4, 2.8m, 0.20m, 5.0m, 1).Value;

        var usersSet = DbSetMockFactory.Create([passenger]);
        var quotesSet = DbSetMockFactory.Create([quote]);
        var driversSet = DbSetMockFactory.Create([driver]);
        var tripsSet = DbSetMockFactory.Create<Trip>([]);
        var paymentsSet = DbSetMockFactory.Create<Payment>([]);
        var vehicleTypesSet = DbSetMockFactory.Create([vehicleType]);

        var context = Substitute.For<IAppDbContext>();
        context.DomainUsers.Returns(usersSet);
        context.PricingQuotes.Returns(quotesSet);
        context.Drivers.Returns(driversSet);
        context.Trips.Returns(tripsSet);
        context.Payments.Returns(paymentsSet);
        context.VehicleTypes.Returns(vehicleTypesSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        return (context, quoteId);
    }

    private static List<CoordinateDto> TwoStops()
        => [new(52.37m, 4.89m, "From"), new(52.38m, 4.90m, "To")];
}
