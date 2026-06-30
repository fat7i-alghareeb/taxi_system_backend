using Taxi.Domain.Payments;
using Xunit;

namespace Taxi.Domain.UnitTests.Payments;

public class PaymentRefundTests
{
    [Fact]
    public void Create_ValidRefund_SetsRequestedStatus()
    {
        var result = CreateRefund(amount: 20m, originalAmount: 100m);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentRefundStatus.Requested, result.Value.Status);
    }

    [Fact]
    public void Create_ValidRefund_StoresFinancialSnapshot()
    {
        var result = CreateRefund(amount: 20.126m, originalAmount: 100.004m, refundPercent: 20.126m);

        Assert.True(result.IsSuccess);
        Assert.Equal(20.13m, result.Value.Amount);
        Assert.Equal(100.00m, result.Value.OriginalPaymentAmountSnapshot);
        Assert.Equal(20.13m, result.Value.RefundPercent);
    }

    [Fact]
    public void Create_AmountEqualToOriginalPayment_MarksFullRefund()
    {
        var result = CreateRefund(amount: 100m, originalAmount: 100m);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsFullRefund);
    }

    [Fact]
    public void Create_AmountGreaterThanOriginalPayment_ReturnsInvalidAmount()
    {
        var result = CreateRefund(amount: 101m, originalAmount: 100m);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.InvalidAmount.Code, result.Error.Code);
    }

    [Fact]
    public void MarkAttemptStarted_IncrementsAttemptCount()
    {
        var refund = CreateRefund().Value;

        refund.MarkAttemptStarted("refund-key-1");

        Assert.Equal(1, refund.AttemptCount);
        Assert.Equal("refund-key-1", refund.IdempotencyKey);
        Assert.NotNull(refund.LastAttemptAtUtc);
    }

    [Fact]
    public void MarkPending_StoresStripeRefundId()
    {
        var refund = CreateRefund().Value;

        refund.MarkPending("re_123", stripePaymentIntentId: "pi_123", stripeChargeId: "ch_123");

        Assert.Equal(PaymentRefundStatus.Pending, refund.Status);
        Assert.Equal("re_123", refund.StripeRefundId);
        Assert.Equal("pi_123", refund.StripePaymentIntentId);
        Assert.Equal("ch_123", refund.StripeChargeId);
    }

    [Fact]
    public void MarkFailed_StoresSafeCustomerMessageAndInternalFailure()
    {
        var refund = CreateRefund().Value;

        refund.MarkFailed("balance_insufficient", "Stripe balance is insufficient.", canRetry: true);

        Assert.Equal(PaymentRefundStatus.Failed, refund.Status);
        Assert.Equal("balance_insufficient", refund.FailureCode);
        Assert.Equal("Stripe balance is insufficient.", refund.FailureReason);
        Assert.True(refund.RequiresAdminAction);
        Assert.True(refund.CanRetry);
        Assert.NotNull(refund.SafeCustomerFailureMessage);
        Assert.NotNull(refund.FailedAtUtc);
    }

    private static Taxi.Domain.Common.Results.Result<PaymentRefund> CreateRefund(
        decimal amount = 20m,
        decimal originalAmount = 100m,
        decimal? refundPercent = 20m)
        => PaymentRefund.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentRefundSourceType.PassengerCancellation,
            amount,
            "eur",
            originalAmount,
            refundPercent,
            tripId: Guid.NewGuid(),
            stripePaymentIntentId: "pi_test",
            stripeChargeId: "ch_test");
}
