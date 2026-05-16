using Taxi.Domain.Payments;
using Xunit;

namespace Taxi.Domain.UnitTests.Payments;

public class PaymentTests
{
    // ── CreateForStripe ──────────────────────────────────────────────────────

    [Fact]
    public void CreateForStripe_ValidAmount_SetsPendingStatus()
    {
        var result = Payment.CreateForStripe(Guid.NewGuid(), Guid.NewGuid(), 10m, "eur", "pi_123", "cs_secret");

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Pending, result.Value.Status);
    }

    [Fact]
    public void CreateForStripe_ValidAmount_SetsCreditCardMethod()
    {
        var result = Payment.CreateForStripe(Guid.NewGuid(), Guid.NewGuid(), 10m, "eur", "pi_123", "cs_secret");

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentMethod.CreditCard, result.Value.Method);
    }

    [Fact]
    public void CreateForStripe_ValidAmount_StoresStripeIds()
    {
        var result = Payment.CreateForStripe(Guid.NewGuid(), Guid.NewGuid(), 10m, "eur", "pi_abc", "cs_xyz");

        Assert.True(result.IsSuccess);
        Assert.Equal("pi_abc", result.Value.StripePaymentIntentId);
        Assert.Equal("cs_xyz", result.Value.StripeClientSecret);
    }

    [Fact]
    public void CreateForStripe_ZeroAmount_ReturnsInvalidAmountError()
    {
        var result = Payment.CreateForStripe(Guid.NewGuid(), Guid.NewGuid(), 0m, "eur", "pi_123", "cs_secret");

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.InvalidAmount.Code, result.Error.Code);
    }

    [Fact]
    public void CreateForStripe_NegativeAmount_ReturnsInvalidAmountError()
    {
        var result = Payment.CreateForStripe(Guid.NewGuid(), Guid.NewGuid(), -5m, "eur", "pi_123", "cs_secret");

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.InvalidAmount.Code, result.Error.Code);
    }

    // ── MarkAsCompleted ──────────────────────────────────────────────────────

    [Fact]
    public void MarkAsCompleted_WhenPending_SetsCompletedStatus()
    {
        var payment = CreateStripePayment();

        payment.MarkAsCompleted("ch_test");

        Assert.Equal(PaymentStatus.Completed, payment.Status);
    }

    [Fact]
    public void MarkAsCompleted_WhenPending_StoresChargeId()
    {
        var payment = CreateStripePayment();

        payment.MarkAsCompleted("ch_xyz");

        Assert.Equal("ch_xyz", payment.StripeChargeId);
        Assert.Equal("ch_xyz", payment.TransactionReference);
    }

    [Fact]
    public void MarkAsCompleted_WhenPending_SetsProcessedAt()
    {
        var payment = CreateStripePayment();

        payment.MarkAsCompleted("ch_test");

        Assert.NotNull(payment.ProcessedAtUtc);
    }

    [Fact]
    public void MarkAsCompleted_WhenAlreadyCompleted_IsIdempotent()
    {
        var payment = CreateStripePayment();
        payment.MarkAsCompleted("ch_first");

        payment.MarkAsCompleted("ch_second");

        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal("ch_first", payment.StripeChargeId);
    }

    // ── MarkAsFailed ─────────────────────────────────────────────────────────

    [Fact]
    public void MarkAsFailed_WhenPending_SetsFailedStatus()
    {
        var payment = CreateStripePayment();

        payment.MarkAsFailed("card_declined", "Your card was declined.");

        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }

    [Fact]
    public void MarkAsFailed_WhenPending_StoresErrorDetails()
    {
        var payment = CreateStripePayment();

        payment.MarkAsFailed("insufficient_funds", "Not enough funds.");

        Assert.Equal("insufficient_funds", payment.LastErrorCode);
        Assert.Equal("Not enough funds.", payment.LastErrorMessage);
    }

    // ── MarkAsRefunded ───────────────────────────────────────────────────────

    [Fact]
    public void MarkAsRefunded_WhenCompleted_SetsRefundedStatus()
    {
        var payment = CreateStripePayment();
        payment.MarkAsCompleted("ch_test");

        payment.MarkAsRefunded();

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
    }

    [Fact]
    public void MarkAsRefunded_WhenAlreadyRefunded_IsIdempotent()
    {
        var payment = CreateStripePayment();
        payment.MarkAsRefunded();

        payment.MarkAsRefunded();

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
    }

    private static Payment CreateStripePayment(decimal amount = 10m)
        => Payment.CreateForStripe(Guid.NewGuid(), Guid.NewGuid(), amount, "eur", "pi_test_123", "cs_test_secret").Value;
}
