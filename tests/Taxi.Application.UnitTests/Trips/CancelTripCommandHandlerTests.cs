using Microsoft.Extensions.Logging;
using NSubstitute;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Commands.CancelTrip;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;
using Taxi.Domain.Vehicles;
using Xunit;

namespace Taxi.Application.UnitTests.Trips;

using TripCoordinate = Taxi.Domain.Trips.Coordinate;

public class CancelTripCommandHandlerTests
{
    private static readonly Guid _passengerId = Guid.NewGuid();
    private const decimal Fare = 20.00m;
    private const string Currency = "eur";

    // ── Policy tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithinFiveMinutes_Returns100PercentRefund()
    {
        var now = DateTimeOffset.UtcNow;
        var (trip, quote, payment) = BuildAcceptedTripWithPayment(now.AddMinutes(-3), Fare);
        var handler = BuildHandler(trip, quote, payment, now);

        var result = await handler.Handle(new CancelTripCommand(trip.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(CancellationReason.PassengerWithinFiveMinutes), result.Value.Cancellation!.Reason);
        Assert.Equal(100m, result.Value.Cancellation.RefundPercent);
        Assert.Equal(Fare, result.Value.Cancellation.RefundAmount);
    }

    [Fact]
    public async Task Handle_AfterFiveMinutes_Returns45PercentRefund()
    {
        var now = DateTimeOffset.UtcNow;
        var (trip, quote, payment) = BuildAcceptedTripWithPayment(now.AddMinutes(-6), Fare);
        var handler = BuildHandler(trip, quote, payment, now);

        var result = await handler.Handle(new CancelTripCommand(trip.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(CancellationReason.PassengerAfterFiveMinutes), result.Value.Cancellation!.Reason);
        Assert.Equal(45m, result.Value.Cancellation.RefundPercent);
        Assert.Equal(Math.Round(Fare * 0.45m, 2, MidpointRounding.AwayFromZero), result.Value.Cancellation.RefundAmount);
    }

    [Fact]
    public async Task Handle_ExactlyAtFiveMinuteBoundary_IsStillFree()
    {
        var now = DateTimeOffset.UtcNow;
        // now == createdAt + 5 min  ⟹  still within free window (inclusive <=)
        var (trip, quote, payment) = BuildAcceptedTripWithPayment(now.AddMinutes(-5), Fare);
        var handler = BuildHandler(trip, quote, payment, now);

        var result = await handler.Handle(new CancelTripCommand(trip.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(CancellationReason.PassengerWithinFiveMinutes), result.Value.Cancellation!.Reason);
        Assert.Equal(100m, result.Value.Cancellation.RefundPercent);
    }

    [Fact]
    public async Task Handle_ScheduledTrip_WithinFiveMinutes_Returns100Percent()
    {
        var now = DateTimeOffset.UtcNow;
        var (trip, quote, payment) = BuildAcceptedTripWithPayment(now.AddMinutes(-2), Fare, scheduledAt: now.AddDays(3));
        var handler = BuildHandler(trip, quote, payment, now);

        var result = await handler.Handle(new CancelTripCommand(trip.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(CancellationReason.PassengerWithinFiveMinutes), result.Value.Cancellation!.Reason);
        Assert.Equal(100m, result.Value.Cancellation.RefundPercent);
    }

    [Fact]
    public async Task Handle_ScheduledTrip_AfterFiveMinutes_Returns45Percent_NoLeadTimeExemption()
    {
        var now = DateTimeOffset.UtcNow;
        // Booked 30 minutes ago; pickup 3 days from now — old lead-time rule would have given 100%.
        var (trip, quote, payment) = BuildAcceptedTripWithPayment(now.AddMinutes(-30), Fare, scheduledAt: now.AddDays(3));
        var handler = BuildHandler(trip, quote, payment, now);

        var result = await handler.Handle(new CancelTripCommand(trip.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(CancellationReason.PassengerAfterFiveMinutes), result.Value.Cancellation!.Reason);
        Assert.Equal(45m, result.Value.Cancellation.RefundPercent);
    }

    [Fact]
    public async Task Handle_AwaitingPayment_Returns0RefundAndCancelsPaymentIntent()
    {
        var now = DateTimeOffset.UtcNow;
        var (trip, quote) = BuildTripWithQuote(now.AddMinutes(-1), Fare);
        // Trip stays in AwaitingPayment (no ConfirmPayment call).
        var payment = Payment.CreateForStripe(Guid.NewGuid(), trip.Id, Fare, Currency, "pi_test_001", "cs_secret").Value;

        var stripe = Substitute.For<IStripePaymentService>();
        stripe.CancelPaymentIntentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success);

        var clientConfig = Substitute.For<IClientConfigProvider>();
        clientConfig.GetClientConfig().Returns(new ClientConfig(StripeEnabled: true, StripePublishableKey: "pk_test", SignalREnabled: false));

        var handler = new CancelTripCommandHandler(
            BuildContext(trip, quote, payment),
            BuildPassengerUser(),
            clientConfig,
            stripe,
            Substitute.For<IRefundLifecycleService>(),
            new FakeTimeProvider(now),
            Substitute.For<ILogger<CancelTripCommandHandler>>());

        var result = await handler.Handle(new CancelTripCommand(trip.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.Cancellation!.RefundPercent);
        Assert.Equal(0m, result.Value.Cancellation.RefundAmount);
        await stripe.Received(1).CancelPaymentIntentAsync(payment.StripePaymentIntentId!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AdminCancel_Returns100PercentRefund_RegardlessOfTiming()
    {
        var now = DateTimeOffset.UtcNow;
        // Booked 1 hour ago — well past the 5-minute window.
        var (trip, quote, payment) = BuildAcceptedTripWithPayment(now.AddHours(-1), Fare);
        var admin = Substitute.For<IUser>();
        admin.Id.Returns(Guid.NewGuid().ToString());
        admin.IsAdmin.Returns(true);
        var handler = BuildHandler(trip, quote, payment, now, user: admin);

        var result = await handler.Handle(new CancelTripCommand(trip.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(CancellationReason.AdminOverride), result.Value.Cancellation!.Reason);
        Assert.Equal(100m, result.Value.Cancellation.RefundPercent);
        Assert.Equal(Fare, result.Value.Cancellation.RefundAmount);
    }

    [Fact]
    public async Task Handle_StripeDisabledCompletedPaymentWithoutIntent_RequestsTrackedRefund()
    {
        var now = DateTimeOffset.UtcNow;
        var (trip, quote, payment) = BuildAcceptedTripWithOfflinePayment(now.AddMinutes(-3), Fare);
        var clientConfig = Substitute.For<IClientConfigProvider>();
        clientConfig.GetClientConfig().Returns(new ClientConfig(false, "pk_test", false));
        var refundLifecycle = Substitute.For<IRefundLifecycleService>();
        refundLifecycle
            .RequestRefundAsync(Arg.Any<RefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<PaymentRefund>(PaymentErrors.RefundUnavailable)));

        var handler = new CancelTripCommandHandler(
            BuildContext(trip, quote, payment),
            BuildPassengerUser(),
            clientConfig,
            Substitute.For<IStripePaymentService>(),
            refundLifecycle,
            new FakeTimeProvider(now),
            Substitute.For<ILogger<CancelTripCommandHandler>>());

        var result = await handler.Handle(new CancelTripCommand(trip.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await refundLifecycle.Received(1).RequestRefundAsync(
            Arg.Is<RefundRequest>(request =>
                request.PaymentId == payment.Id &&
                request.TripId == trip.Id &&
                request.Amount == Fare &&
                request.SourceType == PaymentRefundSourceType.PassengerCancellation),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StripeDisabledTrackedRefundFailure_ReturnsRefundState()
    {
        var now = DateTimeOffset.UtcNow;
        var (trip, quote, payment) = BuildAcceptedTripWithOfflinePayment(now.AddMinutes(-3), Fare);
        var trackedRefund = CreateFailedRefund(payment, trip.Id, Fare);
        var clientConfig = Substitute.For<IClientConfigProvider>();
        clientConfig.GetClientConfig().Returns(new ClientConfig(false, "pk_test", false));
        var refundLifecycle = Substitute.For<IRefundLifecycleService>();
        refundLifecycle
            .RequestRefundAsync(Arg.Any<RefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<PaymentRefund>>(trackedRefund));

        var handler = new CancelTripCommandHandler(
            BuildContext(trip, quote, payment),
            BuildPassengerUser(),
            clientConfig,
            Substitute.For<IStripePaymentService>(),
            refundLifecycle,
            new FakeTimeProvider(now),
            Substitute.For<ILogger<CancelTripCommandHandler>>());

        var result = await handler.Handle(new CancelTripCommand(trip.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Refund);
        Assert.Equal("Failed", result.Value.Refund.Status);
        Assert.Equal(Fare, result.Value.Refund.Amount);
        Assert.Equal(Currency, result.Value.Refund.CurrencyCode);
    }

    [Fact]
    public async Task Handle_ArrivedStatus_Returns45PercentRefund_NoFlatFee()
    {
        var now = DateTimeOffset.UtcNow;
        // Booked 20 minutes ago — past the 5-minute free window.
        var (trip, quote, payment) = BuildArrivedTripWithPayment(now.AddMinutes(-20), now, Fare);
        var handler = BuildHandler(trip, quote, payment, now);

        var result = await handler.Handle(new CancelTripCommand(trip.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(CancellationReason.PassengerAfterFiveMinutes), result.Value.Cancellation!.Reason);
        Assert.Equal(45m, result.Value.Cancellation.RefundPercent);
        Assert.Equal(Math.Round(Fare * 0.45m, 2, MidpointRounding.AwayFromZero), result.Value.Cancellation.RefundAmount);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static (Trip trip, PricingQuote quote, Payment payment) BuildAcceptedTripWithPayment(
        DateTimeOffset bookedAt,
        decimal fare,
        DateTimeOffset? scheduledAt = null)
    {
        var (trip, quote) = BuildTripWithQuote(bookedAt, fare, scheduledAt);
        trip.ConfirmPayment();
        trip.AcceptByAdmin(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var payment = Payment.CreateForStripe(Guid.NewGuid(), trip.Id, fare, Currency, "pi_test_accepted", "cs_secret").Value;
        payment.MarkAsCompleted("ch_test_001");

        return (trip, quote, payment);
    }

    private static (Trip trip, PricingQuote quote, Payment payment) BuildAcceptedTripWithOfflinePayment(
        DateTimeOffset bookedAt,
        decimal fare)
    {
        var (trip, quote) = BuildTripWithQuote(bookedAt, fare);
        trip.ConfirmPayment();
        trip.AcceptByAdmin(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var payment = Payment.Create(Guid.NewGuid(), trip.Id, fare, Currency, PaymentMethod.CreditCard).Value;
        payment.MarkAsCompleted();

        return (trip, quote, payment);
    }

    private static PaymentRefund CreateFailedRefund(Payment payment, Guid tripId, decimal amount)
    {
        var refund = PaymentRefund.Create(
            Guid.NewGuid(),
            payment.Id,
            PaymentRefundSourceType.PassengerCancellation,
            amount,
            payment.Currency,
            payment.Amount,
            100m,
            true,
            tripId,
            passengerId: _passengerId).Value;

        refund.MarkAttemptStarted("test_refund_attempt", DateTimeOffset.UtcNow);
        refund.MarkFailed("refund_failed", "Refund could not be processed.", canRetry: true);
        return refund;
    }

    private static (Trip trip, PricingQuote quote, Payment payment) BuildArrivedTripWithPayment(
        DateTimeOffset bookedAt,
        DateTimeOffset now,
        decimal fare)
    {
        var (trip, quote) = BuildTripWithQuote(bookedAt, fare);
        trip.ConfirmPayment();
        trip.AcceptByAdmin(Guid.NewGuid(), now);
        trip.DriverEnRoute(now, forceOverride: true);
        trip.DriverArrived(now);

        var payment = Payment.CreateForStripe(Guid.NewGuid(), trip.Id, fare, Currency, "pi_test_arrived", "cs_secret").Value;
        payment.MarkAsCompleted("ch_test_002");

        return (trip, quote, payment);
    }

    private static (Trip trip, PricingQuote quote) BuildTripWithQuote(
        DateTimeOffset bookedAt,
        decimal fare,
        DateTimeOffset? scheduledAt = null)
    {
        var quoteId = Guid.NewGuid();
        var vehicleTypeId = Guid.NewGuid();
        var quote = PricingQuote.Create(
            quoteId, _passengerId, vehicleTypeId,
            5m, 10m, fare, fare, 0m, Currency,
            DateTime.UtcNow.AddHours(2),
            [new TripCoordinate(52.37m, 4.89m), new TripCoordinate(52.38m, 4.90m)]).Value;

        var stops = new[]
        {
            TripStop.Create(new TripCoordinate(52.37m, 4.89m), 0, "From").Value,
            TripStop.Create(new TripCoordinate(52.38m, 4.90m), 1, "To").Value,
        };

        var trip = Trip.Request(
            Guid.NewGuid(), "TRP-POLICY-TEST", _passengerId, quote, stops, scheduledAt).Value;

        trip.CreatedAtUtc = bookedAt;

        return (trip, quote);
    }

    private static CancelTripCommandHandler BuildHandler(
        Trip trip,
        PricingQuote quote,
        Payment payment,
        DateTimeOffset fakeNow,
        IUser? user = null)
    {
        var clientConfig = Substitute.For<IClientConfigProvider>();
        clientConfig.GetClientConfig().Returns(new ClientConfig(StripeEnabled: true, StripePublishableKey: "pk_test", SignalREnabled: false));

        var refundLifecycle = Substitute.For<IRefundLifecycleService>();
        refundLifecycle
            .RequestRefundAsync(Arg.Any<RefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<PaymentRefund>(Error.Unexpected("Test.Refund", "test refund mock"))));

        return new CancelTripCommandHandler(
            BuildContext(trip, quote, payment),
            user ?? BuildPassengerUser(),
            clientConfig,
            Substitute.For<IStripePaymentService>(),
            refundLifecycle,
            new FakeTimeProvider(fakeNow),
            Substitute.For<ILogger<CancelTripCommandHandler>>());
    }

    private static IAppDbContext BuildContext(Trip trip, PricingQuote quote, Payment payment)
    {
        var tripsSet = DbSetMockFactory.Create([trip]);
        var quotesSet = DbSetMockFactory.Create([quote]);
        var paymentsSet = DbSetMockFactory.Create([payment]);
        var cancellationsSet = DbSetMockFactory.Create(new List<TripCancellation>());
        var vehicleTypesSet = DbSetMockFactory.Create(new List<VehicleType>());
        var driversSet = DbSetMockFactory.Create(new List<Domain.Drivers.Driver>());
        var context = Substitute.For<IAppDbContext>();
        context.Trips.Returns(tripsSet);
        context.PricingQuotes.Returns(quotesSet);
        context.Payments.Returns(paymentsSet);
        context.TripCancellations.Returns(cancellationsSet);
        context.VehicleTypes.Returns(vehicleTypesSet);
        context.Drivers.Returns(driversSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        return context;
    }

    private static IUser BuildPassengerUser()
    {
        var user = Substitute.For<IUser>();
        user.Id.Returns(_passengerId.ToString());
        user.IsAdmin.Returns(false);
        return user;
    }

    // ── FakeTimeProvider ───────────────────────────────────────────────────────

    private sealed class FakeTimeProvider(DateTimeOffset fixedNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => fixedNow;
    }
}
