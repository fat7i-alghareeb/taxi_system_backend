using Microsoft.Extensions.Logging;
using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Payments.Services;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;

using Xunit;

namespace Taxi.Application.UnitTests.Payments;

public class RefundLifecycleServiceTests
{
    private readonly IClientConfigProvider clientConfig = Substitute.For<IClientConfigProvider>();
    private readonly IRefundProcessingOptionsProvider options = Substitute.For<IRefundProcessingOptionsProvider>();
    private readonly IStripePaymentService stripe = Substitute.For<IStripePaymentService>();
    private readonly IWalletService wallet = Substitute.For<IWalletService>();
    private readonly INotificationService notifications = Substitute.For<INotificationService>();
    private readonly ILogger<RefundLifecycleService> logger = Substitute.For<ILogger<RefundLifecycleService>>();

    public RefundLifecycleServiceTests()
    {
        clientConfig.GetClientConfig().Returns(new ClientConfig(true, "pk_test", true));
        options.GetOptions().Returns(new RefundProcessingOptions(false));
    }

    [Fact]
    public async Task RequestRefundAsync_StripeAccepts_CreatesTrackedPendingRefund()
    {
        var tripCancellationId = Guid.NewGuid();
        var passengerId = Guid.NewGuid();
        var payment = CreateCompletedPayment();
        var context = BuildContext(payment);
        stripe.CreateRefundAsync(
                payment.StripePaymentIntentId!,
                20m,
                Arg.Any<CancellationToken>(),
                Arg.Is<string>(key =>
                    key.Contains(nameof(PaymentRefundSourceType.PassengerCancellation), StringComparison.Ordinal) &&
                    key.Contains(tripCancellationId.ToString("N"), StringComparison.Ordinal)))
            .Returns(new StripeRefundResult(
                "re_test_123",
                20m,
                "EUR",
                "pending",
                payment.StripePaymentIntentId,
                payment.StripeChargeId));
        var service = CreateService(context);

        var result = await service.RequestRefundAsync(
            new RefundRequest(
                payment.Id,
                20m,
                PaymentRefundSourceType.PassengerCancellation,
                20m,
                false,
                payment.TripId,
                tripCancellationId,
                PassengerId: passengerId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Pending, result.Value.Status);
        Assert.Equal("re_test_123", result.Value.StripeRefundId);
        Assert.Equal(payment.StripePaymentIntentId, result.Value.StripePaymentIntentId);
        Assert.Equal(payment.StripeChargeId, result.Value.StripeChargeId);
        Assert.Equal(1, result.Value.AttemptCount);
        context.PaymentRefunds.Received(1).Add(Arg.Is<PaymentRefund>(refund =>
            refund.PaymentId == payment.Id &&
            refund.TripCancellationId == tripCancellationId &&
            refund.PassengerId == passengerId));
    }

    [Fact]
    public async Task RequestRefundAsync_StripeRejects_PersistsFailedRefundAndNotifiesAdmins()
    {
        var payment = CreateCompletedPayment();
        var context = BuildContext(payment);
        stripe.CreateRefundAsync(
                payment.StripePaymentIntentId!,
                25m,
                Arg.Any<CancellationToken>(),
                Arg.Any<string?>())
            .Returns(PaymentErrors.StripeInitiationFailed);
        var service = CreateService(context);

        var result = await service.RequestRefundAsync(
            new RefundRequest(
                payment.Id,
                25m,
                PaymentRefundSourceType.DriverCancellation,
                25m,
                false,
                payment.TripId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Failed, result.Value.Status);
        Assert.True(result.Value.RequiresAdminAction);
        Assert.True(result.Value.CanRetry);
        Assert.Equal(PaymentErrors.StripeInitiationFailed.Code, result.Value.FailureCode);
        Assert.Equal(LocalizationKeys.Payment.RefundCustomerFailureMessage, result.Value.SafeCustomerFailureMessage);
        await notifications.Received(1).SendPushNotificationToAdminsAsync(
            LocalizationKeys.Payment.RefundFailedAdminTitle,
            LocalizationKeys.Payment.RefundFailedAdminBody,
            Arg.Is<Dictionary<string, string>>(data =>
                data["type"] == "refund_failed" &&
                data["refundId"] == result.Value.Id.ToString() &&
                data["paymentId"] == payment.Id.ToString()),
            Arg.Any<CancellationToken>(),
            Arg.Is<object[]?>(args => args == null),
            Arg.Is<object[]?>(args => args != null && args.Length == 3));
    }

    [Fact]
    public async Task RequestRefundAsync_WalletPayment_ReversesToWalletLedgerNotStripe()
    {
        var passengerId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var payment = Payment.CreateWaitingFeeWalletPayment(Guid.NewGuid(), tripId, 8m, "EUR", "wtxn-1").Value;
        payment.MarkAsCompleted();
        var context = BuildContext(payment);

        wallet.CreditRefundReversalAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
                Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<Success>>(Result.Success));

        var service = CreateService(context);

        var result = await service.RequestRefundAsync(
            new RefundRequest(
                payment.Id,
                8m,
                PaymentRefundSourceType.ManualIncidentRefund,
                null,
                true,
                tripId,
                PassengerId: passengerId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Succeeded, result.Value.Status);
        Assert.Null(result.Value.StripeRefundId);
        // Wallet money is reversed to the wallet ledger; no Stripe refund is attempted.
        await wallet.Received(1).CreditRefundReversalAsync(
            passengerId, tripId, payment.Id, Arg.Any<Guid>(), 8m, "EUR",
            Arg.Any<string>(), Arg.Any<CancellationToken>());
        await stripe.DidNotReceive().CreateRefundAsync(
            Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<CancellationToken>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task RequestRefundAsync_ManualIncidentAfterFullRefund_IsBlocked()
    {
        var payment = CreateCompletedPayment();
        var existingRefund = CreateSucceededRefund(payment, 100m, PaymentRefundSourceType.PassengerCancellation);
        var context = BuildContext(payment, [existingRefund]);
        var service = CreateService(context);

        var result = await service.RequestRefundAsync(
            new RefundRequest(
                payment.Id,
                10m,
                PaymentRefundSourceType.ManualIncidentRefund,
                10m,
                false,
                payment.TripId,
                CustomerIncidentId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.RefundFullyRefunded.Code, result.Error.Code);
        await stripe.DidNotReceive().CreateRefundAsync(
            Arg.Any<string>(),
            Arg.Any<decimal?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task RequestRefundAsync_DuplicateActiveRefundForSameSource_IsBlocked()
    {
        var payment = CreateCompletedPayment();
        var cancellationId = Guid.NewGuid();
        var existingRefund = CreatePendingRefund(
            payment,
            20m,
            PaymentRefundSourceType.PassengerCancellation,
            tripCancellationId: cancellationId);
        var context = BuildContext(payment, [existingRefund]);
        var service = CreateService(context);

        var result = await service.RequestRefundAsync(
            new RefundRequest(
                payment.Id,
                20m,
                PaymentRefundSourceType.PassengerCancellation,
                20m,
                false,
                payment.TripId,
                cancellationId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.RefundDuplicate.Code, result.Error.Code);
        await stripe.DidNotReceive().CreateRefundAsync(
            Arg.Any<string>(),
            Arg.Any<decimal?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task RequestRefundAsync_PendingRefundReservesRefundableBalance()
    {
        var payment = CreateCompletedPayment();
        var existingRefund = CreatePendingRefund(payment, 80m, PaymentRefundSourceType.PassengerCancellation);
        var context = BuildContext(payment, [existingRefund]);
        var service = CreateService(context);

        var result = await service.RequestRefundAsync(
            new RefundRequest(
                payment.Id,
                30m,
                PaymentRefundSourceType.ManualIncidentRefund,
                30m,
                false,
                payment.TripId,
                CustomerIncidentId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.RefundExceedsAvailable(30m, 20m).Code, result.Error.Code);
        await stripe.DidNotReceive().CreateRefundAsync(
            Arg.Any<string>(),
            Arg.Any<decimal?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task RequestRefundAsync_ForcedFailure_DoesNotCallStripeAndNotifiesAdmins()
    {
        var payment = CreateCompletedPayment();
        var context = BuildContext(payment);
        options.GetOptions().Returns(new RefundProcessingOptions(true));
        var service = CreateService(context);

        var result = await service.RequestRefundAsync(
            new RefundRequest(
                payment.Id,
                15m,
                PaymentRefundSourceType.AdminCancellation,
                15m,
                false,
                payment.TripId,
                RequestedByAdminId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Failed, result.Value.Status);
        Assert.Equal(PaymentErrors.RefundForcedFailure.Code, result.Value.FailureCode);
        Assert.Equal(LocalizationKeys.Payment.RefundCustomerFailureMessage, result.Value.SafeCustomerFailureMessage);
        await stripe.DidNotReceive().CreateRefundAsync(
            Arg.Any<string>(),
            Arg.Any<decimal?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>());
        await notifications.Received(1).SendPushNotificationToAdminsAsync(
            LocalizationKeys.Payment.RefundFailedAdminTitle,
            LocalizationKeys.Payment.RefundFailedAdminBody,
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>(),
            Arg.Is<object[]?>(args => args == null),
            Arg.Is<object[]?>(args => args != null && args.Length == 3));
    }

    [Fact]
    public async Task RequestRefundAsync_StripeDisabled_CreatesTrackedFailedRefundAndNotifiesAdmins()
    {
        var payment = CreateCompletedPayment();
        var context = BuildContext(payment);
        clientConfig.GetClientConfig().Returns(new ClientConfig(false, "pk_test", true));
        var service = CreateService(context);

        var result = await service.RequestRefundAsync(
            new RefundRequest(
                payment.Id,
                15m,
                PaymentRefundSourceType.PassengerCancellation,
                15m,
                false,
                payment.TripId,
                TripCancellationId: Guid.NewGuid(),
                PassengerId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Failed, result.Value.Status);
        Assert.Equal(PaymentErrors.RefundUnavailable.Code, result.Value.FailureCode);
        Assert.True(result.Value.RequiresAdminAction);
        Assert.True(result.Value.CanRetry);
        Assert.Equal(1, result.Value.AttemptCount);
        Assert.Equal(LocalizationKeys.Payment.RefundCustomerFailureMessage, result.Value.SafeCustomerFailureMessage);
        context.PaymentRefunds.Received(1).Add(Arg.Any<PaymentRefund>());
        await stripe.DidNotReceive().CreateRefundAsync(
            Arg.Any<string>(),
            Arg.Any<decimal?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>());
        await notifications.Received(1).SendPushNotificationToAdminsAsync(
            LocalizationKeys.Payment.RefundFailedAdminTitle,
            LocalizationKeys.Payment.RefundFailedAdminBody,
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>(),
            Arg.Is<object[]?>(args => args == null),
            Arg.Is<object[]?>(args => args != null && args.Length == 3));
    }

    [Fact]
    public async Task RequestRefundAsync_StripeDisabled_AllowsCompletedPaymentWithoutStripeIntent()
    {
        var payment = CreateCompletedOfflinePayment();
        var context = BuildContext(payment);
        clientConfig.GetClientConfig().Returns(new ClientConfig(false, "pk_test", true));
        var service = CreateService(context);

        var result = await service.RequestRefundAsync(
            new RefundRequest(
                payment.Id,
                15m,
                PaymentRefundSourceType.PassengerCancellation,
                15m,
                false,
                payment.TripId,
                TripCancellationId: Guid.NewGuid(),
                PassengerId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Failed, result.Value.Status);
        Assert.True(result.Value.RequiresAdminAction);
        Assert.True(result.Value.CanRetry);
        Assert.Null(result.Value.StripePaymentIntentId);
        context.PaymentRefunds.Received(1).Add(Arg.Any<PaymentRefund>());
        await stripe.DidNotReceive().CreateRefundAsync(
            Arg.Any<string>(),
            Arg.Any<decimal?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task RetryRefundAsync_StripeDisabled_CompletesRetryWithoutCallingStripe()
    {
        var payment = CreateCompletedPayment();
        var failedRefund = CreateFailedRefund(payment, 20m, PaymentRefundSourceType.PassengerCancellation);
        var context = BuildContext(payment, [failedRefund]);
        clientConfig.GetClientConfig().Returns(new ClientConfig(false, "pk_test", true));
        var service = CreateService(context);

        var result = await service.RetryRefundAsync(
            failedRefund.Id,
            Guid.NewGuid(),
            "retry from admin",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Succeeded, result.Value.Status);
        Assert.False(result.Value.RequiresAdminAction);
        Assert.False(result.Value.CanRetry);
        Assert.Null(result.Value.FailureCode);
        Assert.Equal(2, result.Value.AttemptCount);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        await stripe.DidNotReceive().CreateRefundAsync(
            Arg.Any<string>(),
            Arg.Any<decimal?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task RetryRefundAsync_StripeDisabled_FullRefundMarksPaymentRefunded()
    {
        var payment = CreateCompletedWaitingFeePayment();
        var failedRefund = CreateFailedRefund(payment, payment.Amount, PaymentRefundSourceType.AdminCancellation);
        var context = BuildContext(payment, [failedRefund]);
        clientConfig.GetClientConfig().Returns(new ClientConfig(false, "pk_test", true));
        var service = CreateService(context);

        var result = await service.RetryRefundAsync(
            failedRefund.Id,
            Guid.NewGuid(),
            "retry full refund",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Succeeded, result.Value.Status);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        await stripe.DidNotReceive().CreateRefundAsync(
            Arg.Any<string>(),
            Arg.Any<decimal?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task RetryRefundAsync_StripeDisabled_IgnoresStoredCanRetryFalseForFailedRefund()
    {
        var payment = CreateCompletedPayment();
        var failedRefund = CreateFailedRefund(
            payment,
            20m,
            PaymentRefundSourceType.PassengerCancellation,
            canRetry: false);
        var context = BuildContext(payment, [failedRefund]);
        clientConfig.GetClientConfig().Returns(new ClientConfig(false, "pk_test", true));
        var service = CreateService(context);

        var result = await service.RetryRefundAsync(
            failedRefund.Id,
            Guid.NewGuid(),
            "retry from admin",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Succeeded, result.Value.Status);
        await stripe.DidNotReceive().CreateRefundAsync(
            Arg.Any<string>(),
            Arg.Any<decimal?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task RetryRefundAsync_WhenBalanceNoLongerCoversRefund_BlocksRetry()
    {
        var payment = CreateCompletedPayment();
        var failedRefund = CreateFailedRefund(payment, 20m, PaymentRefundSourceType.ManualIncidentRefund);
        var successfulRefund = CreateSucceededRefund(payment, 90m, PaymentRefundSourceType.DriverCancellation);
        var context = BuildContext(payment, [failedRefund, successfulRefund]);
        var service = CreateService(context);

        var result = await service.RetryRefundAsync(
            failedRefund.Id,
            Guid.NewGuid(),
            "retry from admin",
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.RefundFullyRefunded.Code, result.Error.Code);
        Assert.False(failedRefund.CanRetry);
        Assert.Equal(PaymentErrors.RefundFullyRefunded.Code, failedRefund.RetryBlockedReason);
        await stripe.DidNotReceive().CreateRefundAsync(
            Arg.Any<string>(),
            Arg.Any<decimal?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task RetryRefundAsync_WhenRefundCannotRetry_BlocksRetry()
    {
        var payment = CreateCompletedPayment();
        var failedRefund = CreateFailedRefund(
            payment,
            20m,
            PaymentRefundSourceType.ManualIncidentRefund,
            canRetry: false);
        var context = BuildContext(payment, [failedRefund]);
        var service = CreateService(context);

        var result = await service.RetryRefundAsync(
            failedRefund.Id,
            Guid.NewGuid(),
            "retry from admin",
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.RefundRetryBlocked.Code, result.Error.Code);
        Assert.False(failedRefund.CanRetry);
        Assert.Equal(PaymentErrors.RefundRetryBlocked.Code, failedRefund.RetryBlockedReason);
        await stripe.DidNotReceive().CreateRefundAsync(
            Arg.Any<string>(),
            Arg.Any<decimal?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task ReconcilePendingRefundAsync_StripeReportsSucceeded_MarksSucceeded()
    {
        var payment = CreateCompletedPayment();
        var pendingRefund = CreatePendingRefund(payment, 20m, PaymentRefundSourceType.PassengerCancellation);
        var context = BuildContext(payment, [pendingRefund]);
        stripe.GetRefundAsync(pendingRefund.StripeRefundId!, Arg.Any<CancellationToken>())
            .Returns(new StripeRefundResult(pendingRefund.StripeRefundId!, 20m, "EUR", "succeeded"));
        var service = CreateService(context);

        var result = await service.ReconcilePendingRefundAsync(pendingRefund.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Succeeded, result.Value.Status);
    }

    [Fact]
    public async Task ReconcilePendingRefundAsync_StripeReportsFailed_MarksFailedAndNotifiesAdmins()
    {
        var payment = CreateCompletedPayment();
        var pendingRefund = CreatePendingRefund(payment, 20m, PaymentRefundSourceType.PassengerCancellation);
        var context = BuildContext(payment, [pendingRefund]);
        stripe.GetRefundAsync(pendingRefund.StripeRefundId!, Arg.Any<CancellationToken>())
            .Returns(new StripeRefundResult(
                pendingRefund.StripeRefundId!, 20m, "EUR", "failed", FailureReason: "insufficient_funds"));
        var service = CreateService(context);

        var result = await service.ReconcilePendingRefundAsync(pendingRefund.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Failed, result.Value.Status);
        Assert.True(result.Value.CanRetry);
        Assert.Equal("insufficient_funds", result.Value.FailureReason);
        await notifications.Received(1).SendPushNotificationToAdminsAsync(
            LocalizationKeys.Payment.RefundFailedAdminTitle,
            LocalizationKeys.Payment.RefundFailedAdminBody,
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>(),
            Arg.Is<object[]?>(args => args == null),
            Arg.Is<object[]?>(args => args != null && args.Length == 3));
    }

    [Fact]
    public async Task ReconcilePendingRefundAsync_StripeStillPending_OnlyTouchesReconciliationCheck()
    {
        var payment = CreateCompletedPayment();
        var pendingRefund = CreatePendingRefund(payment, 20m, PaymentRefundSourceType.PassengerCancellation);
        var context = BuildContext(payment, [pendingRefund]);
        stripe.GetRefundAsync(pendingRefund.StripeRefundId!, Arg.Any<CancellationToken>())
            .Returns(new StripeRefundResult(pendingRefund.StripeRefundId!, 20m, "EUR", "pending"));
        var service = CreateService(context);

        var result = await service.ReconcilePendingRefundAsync(pendingRefund.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Pending, result.Value.Status);
        Assert.NotNull(result.Value.LastReconciledAtUtc);
    }

    private static Payment CreateCompletedPayment(decimal amount = 100m)
    {
        var payment = Payment.CreateForStripe(
            Guid.NewGuid(),
            Guid.NewGuid(),
            amount,
            "eur",
            "pi_test_123",
            "cs_test_secret").Value;
        payment.MarkAsCompleted("ch_test_123", "card");
        return payment;
    }

    private static Payment CreateCompletedOfflinePayment(decimal amount = 100m)
    {
        var payment = Payment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            amount,
            "eur",
            PaymentMethod.CreditCard).Value;
        payment.MarkAsCompleted();
        return payment;
    }

    private static Payment CreateCompletedWaitingFeePayment(decimal amount = 100m)
    {
        var payment = Payment.CreateWaitingFeeSurcharge(
            Guid.NewGuid(),
            Guid.NewGuid(),
            amount,
            "eur",
            "pi_waiting_fee").Value;
        payment.MarkAsCompleted("ch_waiting_fee", "card");
        return payment;
    }

    private static PaymentRefund CreatePendingRefund(
        Payment payment,
        decimal amount,
        PaymentRefundSourceType sourceType,
        Guid? tripCancellationId = null)
    {
        var refund = PaymentRefund.Create(
            Guid.NewGuid(),
            payment.Id,
            sourceType,
            amount,
            payment.Currency,
            payment.Amount,
            tripId: payment.TripId,
            tripCancellationId: tripCancellationId,
            stripePaymentIntentId: payment.StripePaymentIntentId,
            stripeChargeId: payment.StripeChargeId,
            idempotencyKey: Guid.NewGuid().ToString("N")).Value;
        refund.MarkAttemptStarted(refund.IdempotencyKey!);
        refund.MarkPending($"re_{Guid.NewGuid():N}", payment.StripePaymentIntentId, payment.StripeChargeId);
        return refund;
    }

    private static PaymentRefund CreateSucceededRefund(
        Payment payment,
        decimal amount,
        PaymentRefundSourceType sourceType)
    {
        var refund = CreatePendingRefund(payment, amount, sourceType);
        refund.MarkSucceeded();
        return refund;
    }

    private static PaymentRefund CreateFailedRefund(
        Payment payment,
        decimal amount,
        PaymentRefundSourceType sourceType,
        bool canRetry = true)
    {
        var refund = PaymentRefund.Create(
            Guid.NewGuid(),
            payment.Id,
            sourceType,
            amount,
            payment.Currency,
            payment.Amount,
            tripId: payment.TripId,
            customerIncidentId: Guid.NewGuid(),
            stripePaymentIntentId: payment.StripePaymentIntentId,
            stripeChargeId: payment.StripeChargeId,
            idempotencyKey: Guid.NewGuid().ToString("N")).Value;
        refund.MarkAttemptStarted(refund.IdempotencyKey!);
        refund.MarkFailed("stripe_failed", "Stripe failed", canRetry: canRetry);
        return refund;
    }

    private IAppDbContext BuildContext(Payment payment, List<PaymentRefund>? refunds = null)
    {
        var paymentsSet = DbSetMockFactory.Create([payment]);
        var refundsSet = DbSetMockFactory.Create(refunds ?? []);
        var context = Substitute.For<IAppDbContext>();
        context.Payments.Returns(paymentsSet);
        context.PaymentRefunds.Returns(refundsSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        return context;
    }

    private RefundLifecycleService CreateService(IAppDbContext context)
        => new(context, clientConfig, options, stripe, wallet, notifications, Substitute.For<ITripNotifier>(), logger);
}
