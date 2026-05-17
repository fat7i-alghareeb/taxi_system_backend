using Microsoft.Extensions.Logging;
using NSubstitute;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Payments.Commands.HandleStripeWebhook;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;
using Xunit;

namespace Taxi.Application.UnitTests.Payments;

public class HandleStripeWebhookCommandHandlerTests
{
    private readonly IStripeWebhookValidator _validator = Substitute.For<IStripeWebhookValidator>();
    private readonly ILogger<HandleStripeWebhookCommandHandler> _logger =
        Substitute.For<ILogger<HandleStripeWebhookCommandHandler>>();

    private HandleStripeWebhookCommandHandler CreateHandler(IAppDbContext context)
        => new(context, _validator, _logger);

    // ── Signature validation ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_InvalidSignature_ReturnsStripeSignatureInvalidError()
    {
        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(PaymentErrors.StripeSignatureInvalid);
        var context = Substitute.For<IAppDbContext>();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.StripeSignatureInvalid.Code, result.Error.Code);
    }

    // ── PaymentIntentSucceeded ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_PaymentIntentSucceeded_PaymentNotFound_ReturnsIntentNotFoundError()
    {
        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent("evt_1", StripeWebhookEventKind.PaymentIntentSucceeded,
                "pi_unknown", null, null, null, null, null, null));

        var emptyPayments = DbSetMockFactory.Create<Payment>([]);
        var context = Substitute.For<IAppDbContext>();
        context.Payments.Returns(emptyPayments);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.StripeIntentNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_PaymentIntentSucceeded_CompletesPayment()
    {
        var trip = PaymentTestBuilders.CreateAwaitingPaymentTrip(Guid.NewGuid(), Guid.NewGuid());
        var payment = PaymentTestBuilders.CreatePendingStripePayment(trip.Id, "pi_test_123");
        var driver = PaymentTestBuilders.CreateActiveDriver();

        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent("evt_1", StripeWebhookEventKind.PaymentIntentSucceeded,
                "pi_test_123", "ch_test_456", 15m, "eur", null, null, null));

        var paymentsSet = DbSetMockFactory.Create([payment]);
        var tripsSet = DbSetMockFactory.Create([trip]);
        var driversSet = DbSetMockFactory.Create([driver]);
        var context = Substitute.For<IAppDbContext>();
        context.Payments.Returns(paymentsSet);
        context.Trips.Returns(tripsSet);
        context.Drivers.Returns(driversSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = CreateHandler(context);

        await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal("ch_test_456", payment.StripeChargeId);
    }

    [Fact]
    public async Task Handle_PaymentIntentSucceeded_ConfirmsTripAndAssignsDriver()
    {
        var trip = PaymentTestBuilders.CreateAwaitingPaymentTrip(Guid.NewGuid(), Guid.NewGuid());
        var payment = PaymentTestBuilders.CreatePendingStripePayment(trip.Id, "pi_test_123");
        var driver = PaymentTestBuilders.CreateActiveDriver();

        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent("evt_1", StripeWebhookEventKind.PaymentIntentSucceeded,
                "pi_test_123", "ch_test_456", 15m, "eur", null, null, null));

        var paymentsSet = DbSetMockFactory.Create([payment]);
        var tripsSet = DbSetMockFactory.Create([trip]);
        var driversSet = DbSetMockFactory.Create([driver]);
        var context = Substitute.For<IAppDbContext>();
        context.Payments.Returns(paymentsSet);
        context.Trips.Returns(tripsSet);
        context.Drivers.Returns(driversSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = CreateHandler(context);

        await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.Equal(TripStatus.DriverAssigned, trip.Status);
        Assert.Equal(driver.UserId, trip.DriverId);
    }

    [Fact]
    public async Task Handle_PaymentIntentSucceeded_AlreadyCompleted_IsIdempotent()
    {
        var trip = PaymentTestBuilders.CreateAwaitingPaymentTrip(Guid.NewGuid(), Guid.NewGuid());
        var payment = PaymentTestBuilders.CreatePendingStripePayment(trip.Id, "pi_test_123");
        payment.MarkAsCompleted("ch_existing");

        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent("evt_1", StripeWebhookEventKind.PaymentIntentSucceeded,
                "pi_test_123", "ch_new", 15m, "eur", null, null, null));

        var paymentsSet = DbSetMockFactory.Create([payment]);
        var context = Substitute.For<IAppDbContext>();
        context.Payments.Returns(paymentsSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("ch_existing", payment.StripeChargeId);
    }

    // ── PaymentIntentFailed ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_PaymentIntentFailed_MarksPaymentAndTripFailed()
    {
        var trip = PaymentTestBuilders.CreateAwaitingPaymentTrip(Guid.NewGuid(), Guid.NewGuid());
        var payment = PaymentTestBuilders.CreatePendingStripePayment(trip.Id, "pi_test_123");

        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent("evt_2", StripeWebhookEventKind.PaymentIntentFailed,
                "pi_test_123", null, null, "eur", "card_declined", "Your card was declined.", null));

        var paymentsSet = DbSetMockFactory.Create([payment]);
        var tripsSet = DbSetMockFactory.Create([trip]);
        var quotesSet = DbSetMockFactory.Create<PricingQuote>([]);
        var context = Substitute.For<IAppDbContext>();
        context.Payments.Returns(paymentsSet);
        context.Trips.Returns(tripsSet);
        context.PricingQuotes.Returns(quotesSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal(TripStatus.PaymentFailed, trip.Status);
    }

    [Fact]
    public async Task Handle_PaymentIntentFailed_AlreadyFailed_IsIdempotent()
    {
        var trip = PaymentTestBuilders.CreateAwaitingPaymentTrip(Guid.NewGuid(), Guid.NewGuid());
        var payment = PaymentTestBuilders.CreatePendingStripePayment(trip.Id, "pi_test_123");
        payment.MarkAsFailed("card_declined", "declined");

        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent("evt_2", StripeWebhookEventKind.PaymentIntentFailed,
                "pi_test_123", null, null, "eur", "card_declined", "Your card was declined.", null));

        var paymentsSet = DbSetMockFactory.Create([payment]);
        var context = Substitute.For<IAppDbContext>();
        context.Payments.Returns(paymentsSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsSuccess);
    }

    // ── PaymentIntentCanceled ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_PaymentIntentCanceled_MarksPaymentAndTripFailed()
    {
        var trip = PaymentTestBuilders.CreateAwaitingPaymentTrip(Guid.NewGuid(), Guid.NewGuid());
        var payment = PaymentTestBuilders.CreatePendingStripePayment(trip.Id, "pi_test_123");

        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent("evt_3", StripeWebhookEventKind.PaymentIntentCanceled,
                "pi_test_123", null, null, "eur", "canceled", null, null));

        var paymentsSet = DbSetMockFactory.Create([payment]);
        var tripsSet = DbSetMockFactory.Create([trip]);
        var quotesSet = DbSetMockFactory.Create<PricingQuote>([]);
        var context = Substitute.For<IAppDbContext>();
        context.Payments.Returns(paymentsSet);
        context.Trips.Returns(tripsSet);
        context.PricingQuotes.Returns(quotesSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal(TripStatus.PaymentFailed, trip.Status);
    }

    // ── ChargeRefunded ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ChargeRefunded_MarksPaymentAndTripRefunded()
    {
        var trip = PaymentTestBuilders.CreateAwaitingPaymentTrip(Guid.NewGuid(), Guid.NewGuid());
        trip.ConfirmPayment();
        trip.AssignDriver(Guid.NewGuid());
        trip.Start();
        trip.Complete();
        var payment = PaymentTestBuilders.CreatePendingStripePayment(trip.Id, "pi_test_123");
        payment.MarkAsCompleted("ch_test");

        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent("evt_4", StripeWebhookEventKind.ChargeRefunded,
                "pi_test_123", "ch_test_456", null, "eur", null, null, 15m));

        var paymentsSet = DbSetMockFactory.Create([payment]);
        var tripsSet = DbSetMockFactory.Create([trip]);
        var context = Substitute.For<IAppDbContext>();
        context.Payments.Returns(paymentsSet);
        context.Trips.Returns(tripsSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(TripStatus.Refunded, trip.Status);
    }

    [Fact]
    public async Task Handle_ChargeRefunded_AlreadyRefunded_IsIdempotent()
    {
        var trip = PaymentTestBuilders.CreateAwaitingPaymentTrip(Guid.NewGuid(), Guid.NewGuid());
        var payment = PaymentTestBuilders.CreatePendingStripePayment(trip.Id, "pi_test_123");
        payment.MarkAsRefunded();

        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent("evt_4", StripeWebhookEventKind.ChargeRefunded,
                "pi_test_123", "ch_test", null, "eur", null, null, 15m));

        var paymentsSet = DbSetMockFactory.Create([payment]);
        var context = Substitute.For<IAppDbContext>();
        context.Payments.Returns(paymentsSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsSuccess);
    }

    // ── Unhandled event ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UnhandledEvent_ReturnsSuccess()
    {
        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent("evt_5", StripeWebhookEventKind.Unhandled,
                null, null, null, null, null, null, null));
        var context = Substitute.For<IAppDbContext>();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsSuccess);
    }
}
