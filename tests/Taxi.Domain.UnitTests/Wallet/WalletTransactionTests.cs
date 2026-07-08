using Taxi.Domain.Wallet;
using Xunit;

namespace Taxi.Domain.UnitTests.Wallet;

public class WalletTransactionTests
{
    private static WalletTransaction CreateValidPendingTopUp(decimal amount = 25m)
        => WalletTransaction.CreatePendingTopUp(
            Guid.NewGuid(),
            Guid.NewGuid(),
            amount,
            "eur",
            "pi_test_123",
            "wallet-topup-key").Value;

    [Fact]
    public void CreatePendingTopUp_StartsPending_AsCreditTopUp()
    {
        var txn = CreateValidPendingTopUp();

        Assert.Equal(WalletTransactionStatus.Pending, txn.Status);
        Assert.Equal(WalletTransactionType.TopUp, txn.Type);
        Assert.Equal(WalletTransactionDirection.Credit, txn.Direction);
        Assert.Equal("EUR", txn.Currency);
        Assert.Equal("pi_test_123", txn.StripePaymentIntentId);
        Assert.Null(txn.BalanceAfter);
        Assert.Null(txn.CompletedAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CreatePendingTopUp_WithNonPositiveAmount_Fails(decimal amount)
    {
        var result = WalletTransaction.CreatePendingTopUp(
            Guid.NewGuid(), Guid.NewGuid(), amount, "EUR", "pi_x", "key");

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.InvalidAmount.Code, result.Error.Code);
    }

    [Fact]
    public void CreatePendingTopUp_WithBlankIdempotencyKey_Fails()
    {
        var result = WalletTransaction.CreatePendingTopUp(
            Guid.NewGuid(), Guid.NewGuid(), 10m, "EUR", "pi_x", "  ");

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.IdempotencyKeyRequired.Code, result.Error.Code);
    }

    [Fact]
    public void MarkCommitted_SetsCommittedStateAndBalanceAfter()
    {
        var txn = CreateValidPendingTopUp();

        var result = txn.MarkCommitted(25m, "ch_test_456");

        Assert.True(result.IsSuccess);
        Assert.Equal(WalletTransactionStatus.Committed, txn.Status);
        Assert.Equal(25m, txn.BalanceAfter);
        Assert.Equal("ch_test_456", txn.StripeChargeId);
        Assert.NotNull(txn.CompletedAtUtc);
    }

    [Fact]
    public void MarkCommitted_IsIdempotent_SecondCallDoesNotChangeBalanceAfter()
    {
        // Simulates a duplicate payment_intent.succeeded webhook arriving for the same top-up.
        var txn = CreateValidPendingTopUp();
        txn.MarkCommitted(25m, "ch_first");

        var second = txn.MarkCommitted(9999m, "ch_second");

        Assert.True(second.IsSuccess);
        Assert.Equal(WalletTransactionStatus.Committed, txn.Status);
        Assert.Equal(25m, txn.BalanceAfter);          // unchanged
        Assert.Equal("ch_first", txn.StripeChargeId);  // unchanged
    }

    [Fact]
    public void CreateTripPaymentHold_StartsPending_AsDebitTripPayment()
    {
        var tripId = Guid.NewGuid();
        var hold = WalletTransaction.CreateTripPaymentHold(
            Guid.NewGuid(), Guid.NewGuid(), 12m, "eur", "trip-hold-key", tripId).Value;

        Assert.Equal(WalletTransactionStatus.Pending, hold.Status);
        Assert.Equal(WalletTransactionType.TripPayment, hold.Type);
        Assert.Equal(WalletTransactionDirection.Debit, hold.Direction);
        Assert.Equal(tripId, hold.TripId);
    }

    [Fact]
    public void MarkReleased_FromPending_SetsReleased()
    {
        var hold = WalletTransaction.CreateTripPaymentHold(
            Guid.NewGuid(), Guid.NewGuid(), 12m, "EUR", "k", Guid.NewGuid()).Value;

        var result = hold.MarkReleased();

        Assert.True(result.IsSuccess);
        Assert.Equal(WalletTransactionStatus.Released, hold.Status);
    }

    [Fact]
    public void MarkReleased_AfterCommitted_IsNoOp()
    {
        var hold = WalletTransaction.CreateTripPaymentHold(
            Guid.NewGuid(), Guid.NewGuid(), 12m, "EUR", "k", Guid.NewGuid()).Value;
        hold.MarkCommitted(0m);

        var result = hold.MarkReleased();

        Assert.True(result.IsSuccess);
        Assert.Equal(WalletTransactionStatus.Committed, hold.Status); // committed stays committed
    }

    [Fact]
    public void MarkFailed_FromPending_SetsFailedStateAndCompletedAt()
    {
        var txn = CreateValidPendingTopUp();

        var result = txn.MarkFailed("card_declined");

        Assert.True(result.IsSuccess);
        Assert.Equal(WalletTransactionStatus.Failed, txn.Status);
        Assert.Equal("card_declined", txn.Description);
        Assert.NotNull(txn.CompletedAtUtc);
    }

    [Fact]
    public void MarkFailed_IsIdempotent_SecondCallDoesNotChangeDescription()
    {
        // Simulates a duplicate payment_intent.payment_failed webhook for the same top-up.
        var txn = CreateValidPendingTopUp();
        txn.MarkFailed("card_declined");

        var second = txn.MarkFailed("insufficient_funds");

        Assert.True(second.IsSuccess);
        Assert.Equal(WalletTransactionStatus.Failed, txn.Status);
        Assert.Equal("card_declined", txn.Description); // unchanged
    }

    [Fact]
    public void MarkFailed_AfterCommitted_IsNoOp()
    {
        // A success webhook already landed; a late/out-of-order failure webhook must never
        // undo a completed top-up.
        var txn = CreateValidPendingTopUp();
        txn.MarkCommitted(25m, "ch_test");

        var result = txn.MarkFailed("card_declined");

        Assert.True(result.IsSuccess);
        Assert.Equal(WalletTransactionStatus.Committed, txn.Status); // committed stays committed
        Assert.Equal(25m, txn.BalanceAfter);
    }
}
