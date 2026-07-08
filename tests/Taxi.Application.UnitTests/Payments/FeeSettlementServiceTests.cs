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

public class FeeSettlementServiceTests
{
    private readonly IWalletService _wallet = Substitute.For<IWalletService>();
    private readonly IStripePaymentService _stripe = Substitute.For<IStripePaymentService>();
    private readonly Guid _tripId = Guid.NewGuid();
    private readonly Guid _passengerId = Guid.NewGuid();

    [Fact]
    public async Task Settle_FullyFromWallet_NoCardCharge()
    {
        StubWalletDebit(5m);
        var context = BuildContext();
        var service = new FeeSettlementService(context, _wallet, _stripe);

        var result = await service.SettleWaitingFeeAsync(_tripId, _passengerId, 5m, "EUR", "waiting-fee-x");

        Assert.True(result.IsSuccess);
        Assert.Equal(5m, result.Value.WalletPaid);
        Assert.Equal(0m, result.Value.CardPaid);
        Assert.Equal(0m, result.Value.Unpaid);
        await _stripe.DidNotReceive().ChargeOffSessionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(),
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Settle_SplitWalletAndCard_CardSucceeds()
    {
        StubWalletDebit(3m);
        StubCardCharge("succeeded", requiresAction: false);
        var context = BuildContext(withDefaultCard: true);
        var service = new FeeSettlementService(context, _wallet, _stripe);

        var result = await service.SettleWaitingFeeAsync(_tripId, _passengerId, 5m, "EUR", "waiting-fee-x");

        Assert.True(result.IsSuccess);
        Assert.Equal(3m, result.Value.WalletPaid);
        Assert.Equal(2m, result.Value.CardPaid);
        Assert.Equal(0m, result.Value.Unpaid);
    }

    [Fact]
    public async Task Settle_CardDeclines_RemainderUnpaid()
    {
        StubWalletDebit(3m);
        StubCardCharge("requires_action", requiresAction: true);
        var context = BuildContext(withDefaultCard: true);
        var service = new FeeSettlementService(context, _wallet, _stripe);

        var result = await service.SettleWaitingFeeAsync(_tripId, _passengerId, 5m, "EUR", "waiting-fee-x");

        Assert.True(result.IsSuccess);
        Assert.Equal(3m, result.Value.WalletPaid);
        Assert.Equal(0m, result.Value.CardPaid);
        Assert.Equal(2m, result.Value.Unpaid);
    }

    [Fact]
    public async Task Settle_NoWalletNoReusableCard_FullyUnpaid()
    {
        StubWalletDebit(0m);
        var context = BuildContext(withCustomer: false); // no Stripe customer, no default card, no fare card
        var service = new FeeSettlementService(context, _wallet, _stripe);

        var result = await service.SettleWaitingFeeAsync(_tripId, _passengerId, 5m, "EUR", "waiting-fee-x");

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.WalletPaid);
        Assert.Equal(0m, result.Value.CardPaid);
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
