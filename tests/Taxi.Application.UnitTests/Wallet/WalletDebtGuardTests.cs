using Microsoft.Extensions.Options;
using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;
using Taxi.Application.Features.Wallet.Common;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Contracts.Common;
using Taxi.Domain.Wallet;

using Xunit;

namespace Taxi.Application.UnitTests.Wallet;

/// <summary>
/// A customer who owes money cannot start another ride, or the debt just compounds. The one
/// exception is a debt too small for Stripe to charge: blocking on a few cents would trap the
/// customer with no way to pay their way out.
/// </summary>
public class WalletDebtGuardTests
{
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task Check_NoWalletAccount_Allows()
    {
        var guard = BuildGuard(account: null);

        Assert.Null(await guard.CheckAsync(_userId));
    }

    [Fact]
    public async Task Check_PositiveBalance_Allows()
    {
        var guard = BuildGuard(AccountWith(25m));

        Assert.Null(await guard.CheckAsync(_userId));
    }

    [Fact]
    public async Task Check_ZeroBalance_Allows()
    {
        var guard = BuildGuard(AccountWith(0m));

        Assert.Null(await guard.CheckAsync(_userId));
    }

    [Fact]
    public async Task Check_InDebt_BlocksAndNamesTheAmount()
    {
        var guard = BuildGuard(AccountWith(-12.50m));

        var error = await guard.CheckAsync(_userId);

        Assert.NotNull(error);
        Assert.Equal(LocalizationKeys.Wallet.OutstandingDebt, error!.Value.Code);
        // The amount is carried so the message can name it rather than saying "you owe something".
        Assert.Contains(error.Value.Args!, a => a is decimal d && d == 12.50m);
    }

    [Fact]
    public async Task Check_DebtBelowTheBlockingThreshold_Allows()
    {
        // Stripe refuses charges this small, so blocking would be a permanent lockout.
        var guard = BuildGuard(AccountWith(-0.20m), minDebtToBlock: 0.50m);

        Assert.Null(await guard.CheckAsync(_userId));
    }

    [Fact]
    public async Task Check_DebtExactlyAtTheThreshold_Blocks()
    {
        var guard = BuildGuard(AccountWith(-0.50m), minDebtToBlock: 0.50m);

        Assert.NotNull(await guard.CheckAsync(_userId));
    }

    private static WalletAccount AccountWith(decimal balance)
    {
        var account = WalletAccount.Create(Guid.NewGuid(), Guid.NewGuid(), "EUR").Value;
        if (balance > 0m)
        {
            account.Credit(balance);
        }
        else if (balance < 0m)
        {
            account.ChargeUncollectableFee(-balance);
        }

        return account;
    }

    private WalletDebtGuard BuildGuard(WalletAccount? account, decimal minDebtToBlock = 0.50m)
    {
        // The guard matches on UserId, so rebuild the account against this test's user.
        var accounts = new List<WalletAccount>();
        if (account is not null)
        {
            var owned = WalletAccount.Create(account.Id, _userId, account.Currency).Value;
            if (account.Balance > 0m)
            {
                owned.Credit(account.Balance);
            }
            else if (account.Balance < 0m)
            {
                owned.ChargeUncollectableFee(-account.Balance);
            }

            accounts.Add(owned);
        }

        var accountsSet = DbSetMockFactory.Create(accounts);
        var context = Substitute.For<IAppDbContext>();
        context.WalletAccounts.Returns(accountsSet);

        var options = Options.Create(new WalletOptions { MinDebtToBlock = minDebtToBlock });
        return new WalletDebtGuard(context, options);
    }
}
