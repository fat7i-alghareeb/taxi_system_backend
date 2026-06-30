using Taxi.Domain.Payments;
using Xunit;

namespace Taxi.Domain.UnitTests.Payments;

public class PaymentRefundAccountingTests
{
    [Fact]
    public void Calculate_NoRefunds_ReturnsFullAmountAvailable()
    {
        var totals = PaymentRefundAccounting.Calculate(100m, []);

        Assert.Equal(0m, totals.SuccessfulAmount);
        Assert.Equal(0m, totals.ReservedAmount);
        Assert.Equal(100m, totals.AvailableAmount);
        Assert.False(totals.IsFullyRefunded);
    }

    [Fact]
    public void Calculate_PartialSuccessfulRefund_ReducesAvailableAmount()
    {
        var refund = CreateRefund(20m);
        refund.MarkSucceeded();

        var totals = PaymentRefundAccounting.Calculate(100m, [refund]);

        Assert.Equal(20m, totals.SuccessfulAmount);
        Assert.Equal(80m, totals.AvailableAmount);
        Assert.False(totals.IsFullyRefunded);
    }

    [Fact]
    public void Calculate_MultiplePartialSuccessfulRefunds_SumsSuccessfulAmount()
    {
        var first = CreateRefund(20m);
        first.MarkSucceeded();
        var second = CreateRefund(30m);
        second.MarkSucceeded();

        var totals = PaymentRefundAccounting.Calculate(100m, [first, second]);

        Assert.Equal(50m, totals.SuccessfulAmount);
        Assert.Equal(50m, totals.AvailableAmount);
    }

    [Fact]
    public void Calculate_PendingRefundCountsAsReservedBalance()
    {
        var refund = CreateRefund(40m);
        refund.MarkPending("re_pending");

        var totals = PaymentRefundAccounting.Calculate(100m, [refund]);

        Assert.Equal(0m, totals.SuccessfulAmount);
        Assert.Equal(40m, totals.ReservedAmount);
        Assert.Equal(60m, totals.AvailableAmount);
    }

    [Fact]
    public void Calculate_FullSuccessfulRefund_MarksFullyRefunded()
    {
        var refund = CreateRefund(100m);
        refund.MarkSucceeded();

        var totals = PaymentRefundAccounting.Calculate(100m, [refund]);

        Assert.Equal(100m, totals.SuccessfulAmount);
        Assert.Equal(0m, totals.AvailableAmount);
        Assert.True(totals.IsFullyRefunded);
    }

    [Fact]
    public void Calculate_FailedRefund_DoesNotReserveBalance()
    {
        var refund = CreateRefund(75m);
        refund.MarkFailed("test_failure", "Synthetic failure.");

        var totals = PaymentRefundAccounting.Calculate(100m, [refund]);

        Assert.Equal(75m, totals.FailedAmount);
        Assert.Equal(100m, totals.AvailableAmount);
        Assert.False(totals.IsFullyRefunded);
    }

    private static PaymentRefund CreateRefund(decimal amount)
        => PaymentRefund.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentRefundSourceType.PassengerCancellation,
            amount,
            "eur",
            100m,
            refundPercent: amount).Value;
}
