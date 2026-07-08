using Taxi.Domain.Wallet;
using Xunit;

namespace Taxi.Domain.UnitTests.Wallet;

public class WalletAccountTests
{
    [Fact]
    public void Create_WithValidData_SucceedsWithZeroBalance()
    {
        var userId = Guid.NewGuid();

        var result = WalletAccount.Create(Guid.NewGuid(), userId, "eur");

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal(0m, result.Value.Balance);
        Assert.Equal("EUR", result.Value.Currency); // normalized to upper-invariant
    }

    [Fact]
    public void Create_WithEmptyUserId_Fails()
    {
        var result = WalletAccount.Create(Guid.NewGuid(), Guid.Empty, "EUR");

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.AccountNotFound.Code, result.Error.Code);
    }

    [Fact]
    public void Create_WithBlankCurrency_Fails()
    {
        var result = WalletAccount.Create(Guid.NewGuid(), Guid.NewGuid(), "  ");

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.CurrencyRequired.Code, result.Error.Code);
    }

    [Fact]
    public void Credit_IncreasesBalance()
    {
        var account = WalletAccount.Create(Guid.NewGuid(), Guid.NewGuid(), "EUR").Value;

        var first = account.Credit(10m);
        var second = account.Credit(5.55m);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(15.55m, account.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Credit_WithNonPositiveAmount_Fails(decimal amount)
    {
        var account = WalletAccount.Create(Guid.NewGuid(), Guid.NewGuid(), "EUR").Value;

        var result = account.Credit(amount);

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.InvalidAmount.Code, result.Error.Code);
        Assert.Equal(0m, account.Balance);
    }

    [Fact]
    public void Debit_ReducesBalance()
    {
        var account = WalletAccount.Create(Guid.NewGuid(), Guid.NewGuid(), "EUR").Value;
        account.Credit(20m);

        var result = account.Debit(7.25m);

        Assert.True(result.IsSuccess);
        Assert.Equal(12.75m, account.Balance);
    }

    [Fact]
    public void Debit_MoreThanBalance_FailsAndKeepsBalance()
    {
        var account = WalletAccount.Create(Guid.NewGuid(), Guid.NewGuid(), "EUR").Value;
        account.Credit(5m);

        var result = account.Debit(10m);

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.InsufficientBalance.Code, result.Error.Code);
        Assert.Equal(5m, account.Balance);
    }

    [Fact]
    public void Debit_WholeBalance_LeavesZero()
    {
        var account = WalletAccount.Create(Guid.NewGuid(), Guid.NewGuid(), "EUR").Value;
        account.Credit(15m);

        var result = account.Debit(15m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, account.Balance);
    }
}
