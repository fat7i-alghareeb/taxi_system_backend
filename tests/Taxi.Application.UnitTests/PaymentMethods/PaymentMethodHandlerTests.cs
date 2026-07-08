using Microsoft.Extensions.Options;
using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;
using Taxi.Application.Features.PaymentMethods.Commands.AddPaymentMethod;
using Taxi.Application.Features.PaymentPreferences.Commands.SetPaymentPreference;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.PaymentMethods;
using Taxi.Domain.Users;

using Xunit;

namespace Taxi.Application.UnitTests.PaymentMethods;

public class PaymentMethodHandlerTests
{
    private readonly IUser _user = Substitute.For<IUser>();
    private readonly IStripePaymentService _stripe = Substitute.For<IStripePaymentService>();
    private readonly Guid _userId = Guid.NewGuid();

    public PaymentMethodHandlerTests()
    {
        _user.Id.Returns(_userId.ToString());
    }

    // ── AddPaymentMethod ─────────────────────────────────────────────────────

    [Fact]
    public async Task AddPaymentMethod_FirstCard_BecomesDefault()
    {
        var methodsSet = DbSetMockFactory.Create(new List<PassengerPaymentMethod>());
        var context = Substitute.For<IAppDbContext>();
        context.PaymentMethods.Returns(methodsSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        _stripe.GetPaymentMethodDetailsAsync("pm_123", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<StripePaymentMethodDetails>>(
                new StripePaymentMethodDetails("visa", "4242", 12, 2030, "Jane Doe")));

        var handler = new AddPaymentMethodCommandHandler(context, _user, _stripe);

        var result = await handler.Handle(new AddPaymentMethodCommand("pm_123", SetAsDefault: false), default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsDefault); // first saved method is default automatically
        Assert.Equal("4242", result.Value.LastFour);
        Assert.Equal("visa", result.Value.CardBrand);
    }

    [Fact]
    public async Task AddPaymentMethod_Duplicate_ReturnsExistingWithoutCallingStripe()
    {
        var existing = PassengerPaymentMethod.Create(
            Guid.NewGuid(), _userId, "pm_123", "visa", "4242", 12, 2030, "Jane Doe").Value;

        var methodsSet = DbSetMockFactory.Create(new List<PassengerPaymentMethod> { existing });
        var context = Substitute.For<IAppDbContext>();
        context.PaymentMethods.Returns(methodsSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new AddPaymentMethodCommandHandler(context, _user, _stripe);

        var result = await handler.Handle(new AddPaymentMethodCommand("pm_123", SetAsDefault: false), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(existing.Id, result.Value.Id);
        await _stripe.DidNotReceive().GetPaymentMethodDetailsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── SetPaymentPreference ─────────────────────────────────────────────────

    [Fact]
    public async Task SetPaymentPreference_InvalidType_ReturnsError()
    {
        var options = Options.Create(new PaymentPreferenceOptions { EnabledMethodTypes = ["card", "ideal"] });
        var context = Substitute.For<IAppDbContext>();

        var handler = new SetPaymentPreferenceCommandHandler(context, _user, options);

        var result = await handler.Handle(new SetPaymentPreferenceCommand("bitcoin"), default);

        Assert.True(result.IsFailure);
        Assert.Equal(LocalizationKeys.PassengerPaymentMethod.PreferredMethodInvalid, result.Error.Code);
    }

    [Fact]
    public async Task SetPaymentPreference_ValidType_NormalizesAndSaves()
    {
        var user = User.Create(_userId, "Jane", "+31612345678", null, UserRole.Passenger).Value;
        var usersSet = DbSetMockFactory.Create(new List<User> { user });
        var context = Substitute.For<IAppDbContext>();
        context.DomainUsers.Returns(usersSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var options = Options.Create(new PaymentPreferenceOptions { EnabledMethodTypes = ["card", "ideal"] });
        var handler = new SetPaymentPreferenceCommandHandler(context, _user, options);

        var result = await handler.Handle(new SetPaymentPreferenceCommand("Card"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("card", user.PreferredPaymentMethodType);
        await context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
