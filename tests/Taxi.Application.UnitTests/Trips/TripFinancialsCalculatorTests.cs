using Taxi.Application.Features.Trips.Common;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

using Xunit;

namespace Taxi.Application.UnitTests.Trips;

/// <summary>
/// Unit tests for the pure trip money-breakdown calculator shared by the
/// customer receipt and the admin trip-financials view. Covers the wallet/card
/// split, the unpaid remainder, refunds, waiting fees, and rounding.
/// </summary>
public class TripFinancialsCalculatorTests
{
    private static readonly Guid TripId = Guid.NewGuid();
    private const string Currency = "eur";

    private static Payment CompletedCardFare(decimal amount)
    {
        var p = Payment.CreateForStripe(Guid.NewGuid(), TripId, amount, Currency, "pi_x", "cs_x").Value;
        p.MarkAsCompleted("ch_x", "card");
        return p;
    }

    private static Payment CompletedWalletFare(decimal amount)
    {
        var p = Payment.CreateFareWalletPayment(Guid.NewGuid(), TripId, amount, Currency, "wtx_x").Value;
        p.MarkAsCompleted();
        return p;
    }

    private static Payment CompletedCardWaitingFee(decimal amount)
    {
        var p = Payment.CreateWaitingFeeSurcharge(Guid.NewGuid(), TripId, amount, Currency, "pi_fee").Value;
        p.MarkAsCompleted("ch_fee", "card");
        return p;
    }

    private static PaymentRefund SucceededRefund(decimal amount, decimal original)
    {
        var r = PaymentRefund.Create(
            Guid.NewGuid(),
            paymentId: Guid.NewGuid(),
            sourceType: PaymentRefundSourceType.PassengerCancellation,
            amount: amount,
            currency: Currency,
            originalPaymentAmountSnapshot: original,
            tripId: TripId).Value;
        r.MarkSucceeded();
        return r;
    }

    // Start clamps a 0 grace back to the default, so to get exactly
    // `billableMinutes` billable we wait grace + billableMinutes total; then
    // EstimatedFee == billableMinutes * rate deterministically.
    private static TripWaitingSession WaitingSession(int billableMinutes, decimal ratePerMinute)
    {
        var start = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var session = TripWaitingSession.Start(
            Guid.NewGuid(), TripId, Guid.NewGuid(), ratePerMinute, startedAtUtc: start).Value;
        session.Stop(start.AddMinutes(TripWaitingSession.DefaultGraceMinutes + billableMinutes));
        return session;
    }

    [Fact]
    public void Compute_CardOnlyFare_AllPaidByCard_NothingUnpaid()
    {
        var result = TripFinancialsCalculator.Compute(
            fareAmount: 15.00m,
            currency: Currency,
            payments: [CompletedCardFare(15.00m)],
            refunds: [],
            waitingSessions: []);

        Assert.Equal(15.00m, result.FareAmount);
        Assert.Equal(0m, result.WaitingFeeAmount);
        Assert.Equal(15.00m, result.TotalCharged);
        Assert.Equal(15.00m, result.CardPaidAmount);
        Assert.Equal(0m, result.WalletPaidAmount);
        Assert.Equal(15.00m, result.TotalPaidAmount);
        Assert.Equal(0m, result.UnpaidAmount);
        Assert.Equal(0m, result.RefundedAmount);
        Assert.Single(result.Payments);
    }

    [Fact]
    public void Compute_MixedPayment_SplitsWalletAndCard()
    {
        var result = TripFinancialsCalculator.Compute(
            fareAmount: 20.00m,
            currency: Currency,
            payments: [CompletedWalletFare(8.00m), CompletedCardFare(12.00m)],
            refunds: [],
            waitingSessions: []);

        Assert.Equal(8.00m, result.WalletPaidAmount);
        Assert.Equal(12.00m, result.CardPaidAmount);
        Assert.Equal(20.00m, result.TotalPaidAmount);
        Assert.Equal(0m, result.UnpaidAmount);
    }

    [Fact]
    public void Compute_WaitingFee_AddsToTotalCharged()
    {
        // 4 billable minutes at 0.50/min = 2.00 waiting fee.
        var result = TripFinancialsCalculator.Compute(
            fareAmount: 15.00m,
            currency: Currency,
            payments: [CompletedCardFare(15.00m)],
            refunds: [],
            waitingSessions: [WaitingSession(4, 0.50m)]);

        Assert.Equal(2.00m, result.WaitingFeeAmount);
        Assert.Equal(17.00m, result.TotalCharged);
        // Only the fare was paid; the waiting fee is still outstanding.
        Assert.Equal(2.00m, result.UnpaidAmount);
    }

    [Fact]
    public void Compute_FeePaidByCard_ClearsUnpaid()
    {
        var result = TripFinancialsCalculator.Compute(
            fareAmount: 15.00m,
            currency: Currency,
            payments: [CompletedCardFare(15.00m), CompletedCardWaitingFee(2.00m)],
            refunds: [],
            waitingSessions: [WaitingSession(4, 0.50m)]);

        Assert.Equal(17.00m, result.TotalCharged);
        Assert.Equal(17.00m, result.CardPaidAmount);
        Assert.Equal(0m, result.UnpaidAmount);
    }

    [Fact]
    public void Compute_PendingPayment_DoesNotCountAsPaid()
    {
        var pending = Payment.CreateForStripe(Guid.NewGuid(), TripId, 15.00m, Currency, "pi_p", "cs_p").Value;

        var result = TripFinancialsCalculator.Compute(
            fareAmount: 15.00m,
            currency: Currency,
            payments: [pending],
            refunds: [],
            waitingSessions: []);

        Assert.Equal(0m, result.CardPaidAmount);
        Assert.Equal(0m, result.TotalPaidAmount);
        Assert.Equal(15.00m, result.UnpaidAmount);
    }

    [Fact]
    public void Compute_SucceededRefund_CountsRefunded_PendingRefundDoesNot()
    {
        var pending = PaymentRefund.Create(
            Guid.NewGuid(), Guid.NewGuid(), PaymentRefundSourceType.PassengerCancellation,
            amount: 3.00m, currency: Currency, originalPaymentAmountSnapshot: 15.00m, tripId: TripId).Value;

        var result = TripFinancialsCalculator.Compute(
            fareAmount: 15.00m,
            currency: Currency,
            payments: [CompletedCardFare(15.00m)],
            refunds: [SucceededRefund(5.00m, 15.00m), pending],
            waitingSessions: []);

        Assert.Equal(5.00m, result.RefundedAmount);
        Assert.Equal(2, result.Refunds.Count);
    }

    [Fact]
    public void Compute_UnpaidNeverNegative_WhenOverpaidOrRounded()
    {
        var result = TripFinancialsCalculator.Compute(
            fareAmount: 10.00m,
            currency: Currency,
            payments: [CompletedCardFare(12.00m)],
            refunds: [],
            waitingSessions: []);

        Assert.Equal(0m, result.UnpaidAmount);
    }

    [Fact]
    public void Compute_RoundsToTwoDecimals_AwayFromZero()
    {
        // 3 billable minutes at 0.155/min = 0.465 → rounds to 0.47 (away from zero).
        var result = TripFinancialsCalculator.Compute(
            fareAmount: 10.005m,
            currency: Currency,
            payments: [],
            refunds: [],
            waitingSessions: [WaitingSession(3, 0.155m)]);

        Assert.Equal(0.47m, result.WaitingFeeAmount);
        Assert.Equal(10.01m, result.FareAmount);
    }
}
