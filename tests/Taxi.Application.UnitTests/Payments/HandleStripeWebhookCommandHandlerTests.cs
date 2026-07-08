using Microsoft.Extensions.Logging;
using NSubstitute;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Payments.Commands.HandleStripeWebhook;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;
using Xunit;

namespace Taxi.Application.UnitTests.Payments;

public class HandleStripeWebhookCommandHandlerTests
{
    private readonly IStripeWebhookValidator _validator = Substitute.For<IStripeWebhookValidator>();
    private readonly IStripePaymentService _stripe = Substitute.For<IStripePaymentService>();
    private readonly IWalletService _wallet = Substitute.For<IWalletService>();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly ILogger<HandleStripeWebhookCommandHandler> _logger =
        Substitute.For<ILogger<HandleStripeWebhookCommandHandler>>();

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
    public async Task Handle_PaymentIntentSucceeded_ConfirmsTripForAdminAcceptance()
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

        Assert.Equal(TripStatus.AwaitingAdminAcceptance, trip.Status);
        Assert.Null(trip.DriverId);
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
        trip.AcceptByAdmin(Guid.NewGuid(), DateTimeOffset.UtcNow);
        trip.DriverEnRoute(DateTimeOffset.UtcNow);
        trip.DriverArrived(DateTimeOffset.UtcNow);
        trip.Start(DateTimeOffset.UtcNow);
        trip.Complete(DateTimeOffset.UtcNow);
        var payment = PaymentTestBuilders.CreatePendingStripePayment(trip.Id, "pi_test_123");
        payment.MarkAsCompleted("ch_test");

        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent("evt_4", StripeWebhookEventKind.ChargeRefunded,
                "pi_test_123", "ch_test_456", null, "eur", null, null, 15m));

        var paymentsSet = DbSetMockFactory.Create([payment]);
        var tripsSet = DbSetMockFactory.Create([trip]);
        var refundsSet = DbSetMockFactory.Create<PaymentRefund>([]);
        var context = Substitute.For<IAppDbContext>();
        context.Payments.Returns(paymentsSet);
        context.Trips.Returns(tripsSet);
        context.PaymentRefunds.Returns(refundsSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(TripStatus.Refunded, trip.Status);
    }

    // ── Refund lifecycle events ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_RefundCreated_MarksTrackedRefundPending()
    {
        var payment = CreateCompletedPayment(100m);
        var refund = CreateFailedRefund(payment, 20m, "re_pending");

        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent(
                "evt_refund_created",
                StripeWebhookEventKind.RefundCreated,
                payment.StripePaymentIntentId,
                payment.StripeChargeId,
                null,
                "eur",
                null,
                null,
                20m,
                "re_pending",
                "pending"));

        var context = BuildRefundWebhookContext(payment, null, [refund]);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Pending, refund.Status);
        Assert.Equal("evt_refund_created", refund.LastStripeEventId);
    }

    [Fact]
    public async Task Handle_RefundUpdatedSucceeded_PartialRefundDoesNotMarkPaymentRefunded()
    {
        var payment = CreateCompletedPayment(100m);
        var trip = PaymentTestBuilders.CreateAwaitingPaymentTrip(Guid.NewGuid(), Guid.NewGuid());
        var refund = CreatePendingRefund(payment, 20m, "re_partial");

        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent(
                "evt_refund_succeeded",
                StripeWebhookEventKind.RefundUpdated,
                payment.StripePaymentIntentId,
                payment.StripeChargeId,
                null,
                "eur",
                null,
                null,
                20m,
                "re_partial",
                "succeeded"));

        var context = BuildRefundWebhookContext(payment, trip, [refund]);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Succeeded, refund.Status);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.NotEqual(TripStatus.Refunded, trip.Status);
    }

    [Fact]
    public async Task Handle_RefundUpdatedSucceeded_FullRefundMarksPaymentRefunded()
    {
        var trip = CreateCompletedTrip();
        var payment = CreateCompletedPayment(100m, trip.Id);
        var refund = CreatePendingRefund(payment, 100m, "re_full");

        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent(
                "evt_refund_full",
                StripeWebhookEventKind.RefundUpdated,
                payment.StripePaymentIntentId,
                payment.StripeChargeId,
                null,
                "eur",
                null,
                null,
                100m,
                "re_full",
                "succeeded"));

        var context = BuildRefundWebhookContext(payment, trip, [refund]);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Succeeded, refund.Status);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(TripStatus.Refunded, trip.Status);
    }

    [Fact]
    public async Task Handle_RefundFailed_MarksRefundFailedAndNotifiesAdmins()
    {
        var payment = CreateCompletedPayment(100m);
        var refund = CreatePendingRefund(payment, 20m, "re_failed");

        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent(
                "evt_refund_failed",
                StripeWebhookEventKind.RefundFailed,
                payment.StripePaymentIntentId,
                payment.StripeChargeId,
                null,
                "eur",
                "lost_or_stolen_card",
                "lost_or_stolen_card",
                20m,
                "re_failed",
                "failed"));

        var context = BuildRefundWebhookContext(payment, null, [refund]);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Failed, refund.Status);
        Assert.True(refund.RequiresAdminAction);
        Assert.True(refund.CanRetry);
        Assert.Equal("lost_or_stolen_card", refund.FailureCode);
        Assert.Equal(LocalizationKeys.Payment.RefundCustomerFailureMessage, refund.SafeCustomerFailureMessage);
        await _notifications.Received(1).SendPushNotificationToAdminsAsync(
            LocalizationKeys.Payment.RefundFailedAdminTitle,
            LocalizationKeys.Payment.RefundFailedAdminBody,
            Arg.Is<Dictionary<string, string>>(data =>
                data["type"] == "refund_failed" &&
                data["refundId"] == refund.Id.ToString()),
            Arg.Any<CancellationToken>(),
            Arg.Is<object[]?>(args => args == null),
            Arg.Is<object[]?>(args => args != null && args.Length == 3));
    }

    [Fact]
    public async Task Handle_RefundWebhookDuplicateEvent_IsIgnored()
    {
        var payment = CreateCompletedPayment(100m);
        var refund = CreatePendingRefund(payment, 20m, "re_duplicate");
        refund.MarkSucceeded("evt_duplicate");

        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent(
                "evt_duplicate",
                StripeWebhookEventKind.RefundFailed,
                payment.StripePaymentIntentId,
                payment.StripeChargeId,
                null,
                "eur",
                "failed",
                "failed",
                20m,
                "re_duplicate",
                "failed"));

        var context = BuildRefundWebhookContext(payment, null, [refund]);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Succeeded, refund.Status);
        await _notifications.DidNotReceive().SendPushNotificationToAdminsAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<object[]?>(),
            Arg.Any<object[]?>());
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

    // ── Wallet top-up crediting ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_PaymentIntentSucceeded_WalletTopUp_Credited_NotifiesAndSucceeds()
    {
        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent("evt_topup", StripeWebhookEventKind.PaymentIntentSucceeded,
                "pi_topup", "ch_topup", 25m, "eur", null, null, null));

        var context = Substitute.For<IAppDbContext>();
        var handler = CreateHandler(context);

        var userId = Guid.NewGuid();
        _wallet.CreditTopUpFromWebhookAsync("pi_topup", 25m, "ch_topup", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<WalletTopUpCreditOutcome>>(
                new WalletTopUpCreditOutcome(WalletTopUpCreditStatus.Credited, userId, 25m, 25m, "EUR")));

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsSuccess);
        await _notifications.Received(1).SendPushNotificationAsync(
            userId,
            LocalizationKeys.Notification.WalletTopUpSucceededTitle,
            LocalizationKeys.Notification.WalletTopUpSucceededBody,
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<object[]?>(),
            Arg.Any<object[]?>());
        // Never touched the trip-payment path.
        _ = context.DidNotReceive().Payments;
    }

    [Fact]
    public async Task Handle_PaymentIntentSucceeded_WalletTopUp_AlreadyCredited_DoesNotNotifyAndSucceeds()
    {
        _validator.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new StripeWebhookEvent("evt_topup_dup", StripeWebhookEventKind.PaymentIntentSucceeded,
                "pi_topup", "ch_topup", 25m, "eur", null, null, null));

        var context = Substitute.For<IAppDbContext>();
        var handler = CreateHandler(context);

        _wallet.CreditTopUpFromWebhookAsync("pi_topup", 25m, "ch_topup", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<WalletTopUpCreditOutcome>>(
                new WalletTopUpCreditOutcome(WalletTopUpCreditStatus.AlreadyCredited, Guid.NewGuid(), 25m, 25m, "EUR")));

        var result = await handler.Handle(new HandleStripeWebhookCommand("json", "sig"), default);

        Assert.True(result.IsSuccess);
        // Duplicate webhook: no second notification, no double credit (idempotent no-op).
        await _notifications.DidNotReceive().SendPushNotificationAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<object[]?>(),
            Arg.Any<object[]?>());
    }

    private HandleStripeWebhookCommandHandler CreateHandler(IAppDbContext context)
    {
        // By default the PaymentIntent is not a wallet top-up, so the handler falls through
        // to its normal trip-payment path. Wallet-specific tests override this.
        _wallet.CreditTopUpFromWebhookAsync(
                Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<WalletTopUpCreditOutcome>>(
                new WalletTopUpCreditOutcome(WalletTopUpCreditStatus.NotAWalletTopUp, Guid.Empty, 0m, 0m, string.Empty)));

        // No wallet hold by default (card-only trips): commit is a no-op, release is a no-op.
        _wallet.CommitTripHoldAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<decimal>>(0m));
        _wallet.ReleaseTripHoldAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<Success>>(Result.Success));

        return new(context, _validator, _stripe, _wallet, _notifications, Substitute.For<ITripNotifier>(), _logger);
    }

    private static Trip CreateCompletedTrip()
    {
        var trip = PaymentTestBuilders.CreateAwaitingPaymentTrip(Guid.NewGuid(), Guid.NewGuid());
        trip.ConfirmPayment();
        trip.AcceptByAdmin(Guid.NewGuid(), DateTimeOffset.UtcNow);
        trip.DriverEnRoute(DateTimeOffset.UtcNow);
        trip.DriverArrived(DateTimeOffset.UtcNow);
        trip.Start(DateTimeOffset.UtcNow);
        trip.Complete(DateTimeOffset.UtcNow);
        return trip;
    }

    private static Payment CreateCompletedPayment(decimal amount, Guid? tripId = null)
    {
        var payment = Payment.CreateForStripe(
            Guid.NewGuid(),
            tripId ?? Guid.NewGuid(),
            amount,
            "eur",
            "pi_test_refund",
            "cs_test_secret").Value;
        payment.MarkAsCompleted("ch_test_refund", "card");
        return payment;
    }

    private static PaymentRefund CreatePendingRefund(Payment payment, decimal amount, string stripeRefundId)
    {
        var refund = PaymentRefund.Create(
            Guid.NewGuid(),
            payment.Id,
            PaymentRefundSourceType.PassengerCancellation,
            amount,
            payment.Currency,
            payment.Amount,
            tripId: payment.TripId,
            stripePaymentIntentId: payment.StripePaymentIntentId,
            stripeChargeId: payment.StripeChargeId,
            idempotencyKey: Guid.NewGuid().ToString("N")).Value;
        refund.MarkAttemptStarted(refund.IdempotencyKey!);
        refund.MarkPending(stripeRefundId, payment.StripePaymentIntentId, payment.StripeChargeId);
        return refund;
    }

    private static PaymentRefund CreateFailedRefund(Payment payment, decimal amount, string stripeRefundId)
    {
        var refund = CreatePendingRefund(payment, amount, stripeRefundId);
        refund.MarkFailed("temporary_failure", "temporary_failure", canRetry: true);
        return refund;
    }

    private static IAppDbContext BuildRefundWebhookContext(
        Payment payment,
        Trip? trip,
        List<PaymentRefund> refunds)
    {
        var paymentsSet = DbSetMockFactory.Create([payment]);
        var trips = trip is null ? new List<Trip>() : [trip];
        var tripsSet = DbSetMockFactory.Create(trips);
        var refundsSet = DbSetMockFactory.Create(refunds);
        var context = Substitute.For<IAppDbContext>();
        context.Payments.Returns(paymentsSet);
        context.Trips.Returns(tripsSet);
        context.PaymentRefunds.Returns(refundsSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        return context;
    }
}
