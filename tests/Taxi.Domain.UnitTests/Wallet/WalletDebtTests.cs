using Taxi.Domain.Wallet;

using Xunit;

namespace Taxi.Domain.UnitTests.Wallet;

/// <summary>
/// A wallet may go negative, but only through one door. A fee the customer already incurred and
/// cannot decline has to land somewhere — before this it simply vanished — whereas a ride is
/// optional and must never be taken on credit. That asymmetry is the whole design, so both halves
/// are pinned here.
/// </summary>
public class WalletDebtTests
{
    private const string Currency = "EUR";

    [Fact]
    public void ChargeUncollectableFee_OnEmptyWallet_GoesNegative()
    {
        var account = NewAccount();

        var result = account.ChargeUncollectableFee(12.50m);

        Assert.True(result.IsSuccess);
        Assert.Equal(-12.50m, account.Balance);
        Assert.True(account.IsInDebt);
        Assert.Equal(12.50m, account.AmountOwed);
    }

    [Fact]
    public void ChargeUncollectableFee_PartialBalance_TakesTheRestIntoDebt()
    {
        var account = NewAccount();
        account.Credit(5m);

        account.ChargeUncollectableFee(12.50m);

        Assert.Equal(-7.50m, account.Balance);
        Assert.Equal(7.50m, account.AmountOwed);
    }

    [Fact]
    public void ChargeUncollectableFee_RejectsNonPositiveAmounts()
    {
        var account = NewAccount();

        Assert.True(account.ChargeUncollectableFee(0m).IsFailure);
        Assert.True(account.ChargeUncollectableFee(-1m).IsFailure);
        Assert.Equal(0m, account.Balance);
    }

    [Fact]
    public void Debit_StillRefusesToOverdraw_SoRidesCannotBeTakenOnCredit()
    {
        var account = NewAccount();
        account.Credit(5m);

        var result = account.Debit(12.50m);

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.InsufficientBalance.Code, result.Error.Code);
        Assert.Equal(5m, account.Balance); // untouched
    }

    [Fact]
    public void Debit_AgainstADebtBalance_IsRefused()
    {
        // A customer already in debt must not be able to spend further.
        var account = NewAccount();
        account.ChargeUncollectableFee(10m);

        var result = account.Debit(1m);

        Assert.True(result.IsFailure);
        Assert.Equal(-10m, account.Balance);
    }

    [Fact]
    public void Credit_NetsAgainstDebt_AndCanRestoreAPositiveBalance()
    {
        // This is how a top-up settles a debt — no special path needed.
        var account = NewAccount();
        account.ChargeUncollectableFee(20m);

        account.Credit(50m);

        Assert.Equal(30m, account.Balance);
        Assert.False(account.IsInDebt);
        Assert.Equal(0m, account.AmountOwed);
    }

    [Fact]
    public void Credit_PartialRepayment_LeavesTheCustomerStillInDebt()
    {
        var account = NewAccount();
        account.ChargeUncollectableFee(20m);

        account.Credit(5m);

        Assert.Equal(-15m, account.Balance);
        Assert.True(account.IsInDebt);
        Assert.Equal(15m, account.AmountOwed);
    }

    [Fact]
    public void ExactRepayment_ClearsTheDebtToZero()
    {
        var account = NewAccount();
        account.ChargeUncollectableFee(12.50m);

        account.Credit(12.50m);

        Assert.Equal(0m, account.Balance);
        Assert.False(account.IsInDebt);
    }

    [Fact]
    public void AmountOwed_IsZero_WhenTheBalanceIsPositive()
    {
        var account = NewAccount();
        account.Credit(10m);

        Assert.False(account.IsInDebt);
        Assert.Equal(0m, account.AmountOwed);
    }

    [Fact]
    public void ChargeUncollectableFee_RoundsToTwoDecimalsAwayFromZero()
    {
        var account = NewAccount();

        account.ChargeUncollectableFee(0.155m);

        Assert.Equal(-0.16m, account.Balance);
    }

    private static WalletAccount NewAccount() =>
        WalletAccount.Create(Guid.NewGuid(), Guid.NewGuid(), Currency).Value;
}
