using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Payments.Services;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Common.Results;
using Taxi.Domain.PaymentMethods;
using Taxi.Domain.Payments;
using Taxi.Domain.Users;

using Xunit;

namespace Taxi.Application.UnitTests.Payments;

/// <summary>
/// The waiting fee settles in a fixed order: wallet, then the saved card, then — because the
/// customer cannot decline a fee they already incurred — whatever is left is charged to the wallet
/// as debt. Nothing may be left silently uncollected: that is what used to lose the money and
/// print a "Remaining due" line on the customer's invoice.
/// </summary>
public class FeeSettlementServiceTests
{
    private readonly IWalletService _wallet = Substitute.For<IWalletService>();
    private readonly IStripePaymentService _stripe = Substitute.For<IStripePaymentService>();
    private readonly IClientConfigProvider _clientConfig = Substitute.For<IClientConfigProvider>();
    private readonly Guid _tripId = Guid.NewGuid();
    private readonly Guid _passengerId = Guid.NewGuid();

    public FeeSettlementServiceTests()
    {
        _clientConfig.GetClientConfig().Returns(new ClientConfig(true, "pk_test", true));
        StubDebtCharge();
    }

    [Fact]
    public async Task Settle_FullyFromWallet_NoCardCharge()
    {
        StubWalletDebit(5m);
        var service = new FeeSettlementService(BuildContext(), _wallet, _stripe, _clientConfig);

        var result = await service.SettleWaitingFeeAsync(_tripId, _passengerId, 5m, "EUR", "waiting-fee-x");

        Assert.True(result.IsSuccess);
        Assert.Equal(5m, result.Value.WalletPaid);
        Assert.Equal(0m, result.Value.CardPaid);
        Assert.Equal(0m, result.Value.Unpaid);
        Assert.Equal(0m, result.Value.ChargedToDebt);
        await _stripe.DidNotReceive().ChargeOffSessionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(),
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Settle_SplitWalletAndCard_CardSucceeds()
    {
        StubWalletDebit(3m);
        StubCardCharge("succeeded", requiresAction: false);
        var service = new FeeSettlementService(
            BuildContext(withDefaultCard: true), _wallet, _stripe, _clientConfig);

        var result = await service.SettleWaitingFeeAsync(_tripId, _passengerId, 5m, "EUR", "waiting-fee-x");

        Assert.True(result.IsSuccess);
        Assert.Equal(3m, result.Value.WalletPaid);
        Assert.Equal(2m, result.Value.CardPaid);
        Assert.Equal(0m, result.Value.Unpaid);
        Assert.Equal(0m, result.Value.ChargedToDebt);
    }

    [Fact]
    public async Task Settle_CardDeclines_RemainderBecomesDebt()
    {
        StubWalletDebit(3m);
        StubCardCharge("requires_action", requiresAction: true);
        var service = new FeeSettlementService(
            BuildContext(withDefaultCard: true), _wallet, _stripe, _clientConfig);

        var result = await service.SettleWaitingFeeAsync(_tripId, _passengerId, 5m, "EUR", "waiting-fee-x");

        Assert.True(result.IsSuccess);
        Assert.Equal(3m, result.Value.WalletPaid);
        Assert.Equal(0m, result.Value.CardPaid);
        Assert.Equal(2m, result.Value.ChargedToDebt);
        Assert.Equal(0m, result.Value.Unpaid); // nothing left uncollected
        Assert.True(result.Value.HasDebt);
    }

    [Fact]
    public async Task Settle_NoWalletNoReusableCard_FullyChargedToDebt()
    {
        StubWalletDebit(0m);
        var service = new FeeSettlementService(
            BuildContext(withCustomer: false), _wallet, _stripe, _clientConfig);

        var result = await service.SettleWaitingFeeAsync(_tripId, _passengerId, 5m, "EUR", "waiting-fee-x");

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.WalletPaid);
        Assert.Equal(0m, result.Value.CardPaid);
        Assert.Equal(5m, result.Value.ChargedToDebt);
        Assert.Equal(0m, result.Value.Unpaid);
    }

    [Fact]
    public async Task Settle_StripeDisabled_StillChargesDebtInsteadOfLosingTheFee()
    {
        // Only the card step needs Stripe. If switching Stripe off skipped the debt step too, the
        // fee would vanish and the invoice would snapshot a remaining balance.
        _clientConfig.GetClientConfig().Returns(new ClientConfig(false, string.Empty, true));
        StubWalletDebit(0m);
        var service = new FeeSettlementService(
            BuildContext(withDefaultCard: true), _wallet, _stripe, _clientConfig);

        var result = await service.SettleWaitingFeeAsync(_tripId, _passengerId, 5m, "EUR", "waiting-fee-x");

        Assert.True(result.IsSuccess);
        Assert.Equal(5m, result.Value.ChargedToDebt);
        Assert.Equal(0m, result.Value.Unpaid);
        await _stripe.DidNotReceive().ChargeOffSessionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(),
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Settle_DebtChargeUsesADistinctIdempotencyKey()
    {
        // The wallet debit and the debt charge must not collide on the same key, or the second
        // would be swallowed as an idempotent replay of the first and the fee would be lost.
        StubWalletDebit(0m);
        var service = new FeeSettlementService(
            BuildContext(withCustomer: false), _wallet, _stripe, _clientConfig);

        await service.SettleWaitingFeeAsync(_tripId, _passengerId, 5m, "EUR", "waiting-fee-x");

        await _wallet.Received(1).ChargeUncollectableFeeAsync(
            _passengerId, _tripId, 5m, "EUR", Arg.Any<string>(),
            "waiting-fee-x-debt", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Settle_DebtChargeFails_RemainderReportedUnpaid()
    {
        StubWalletDebit(0m);
        _wallet.ChargeUncollectableFeeAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<WalletDebitOutcome>>(Taxi.Domain.Wallet.WalletErrors.AccountNotFound));
        var service = new FeeSettlementService(
            BuildContext(withCustomer: false), _wallet, _stripe, _clientConfig);

        var result = await service.SettleWaitingFeeAsync(_tripId, _passengerId, 5m, "EUR", "waiting-fee-x");

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.ChargedToDebt);
        Assert.Equal(5m, result.Value.Unpaid);
    }

    private void StubWalletDebit(decimal debited)
    {
        _wallet.DebitForFeeAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<WalletDebitOutcome>>(
                new WalletDebitOutcome(debited, debited > 0m ? Guid.NewGuid() : null)));
    }

    private void StubDebtCharge()
    {
        _wallet.ChargeUncollectableFeeAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<Result<WalletDebitOutcome>>(
                new WalletDebitOutcome(call.ArgAt<decimal>(2), Guid.NewGuid())));
    }

    private void StubCardCharge(string status, bool requiresAction)
    {
        _stripe.ChargeOffSessionAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<StripeSurchargeResult>>(
                new StripeSurchargeResult("pi_fee", status, requiresAction ? null : "ch_fee", requiresAction)));
    }

    private IAppDbContext BuildContext(bool withDefaultCard = false, bool withCustomer = true)
    {
        var user = User.Create(_passengerId, "Jane", "+31612345678", null, UserRole.Passenger).Value;
        if (withCustomer)
        {
            user.SetStripeCustomerId("cus_test");
        }

        var methods = new List<PassengerPaymentMethod>();
        if (withDefaultCard)
        {
            var method = PassengerPaymentMethod.Create(
                Guid.NewGuid(), _passengerId, "pm_default", "visa", "4242", 12, 2030, null).Value;
            method.SetAsDefault();
            methods.Add(method);
        }

        var usersSet = DbSetMockFactory.Create(new List<User> { user });
        var methodsSet = DbSetMockFactory.Create(methods);
        var paymentsSet = DbSetMockFactory.Create(new List<Payment>());

        var context = Substitute.For<IAppDbContext>();
        context.DomainUsers.Returns(usersSet);
        context.PaymentMethods.Returns(methodsSet);
        context.Payments.Returns(paymentsSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        return context;
    }
}
