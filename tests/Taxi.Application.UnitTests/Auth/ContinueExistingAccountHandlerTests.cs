using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Commands.ContinueExistingAccount;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Users;

using Xunit;

namespace Taxi.Application.UnitTests.Auth;

public class ContinueExistingAccountHandlerTests
{
    [Fact]
    public async Task Handle_AccountWithEmail_SendsWelcomeBackEmail_AndSucceeds()
    {
        var user = User.Create(
            Guid.NewGuid(), "Ada", "+31612345678", "ada@example.com", UserRole.Passenger,
            isPhoneVerified: false, isEmailVerified: true).Value;

        var (handler, welcomeEmailService) = BuildHandler(new List<User> { user }, user.Id);

        var result = await handler.Handle(new ContinueExistingAccountCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await welcomeEmailService.Received(1).SendWelcomeBackEmailAsync(
            "ada@example.com", user.Name, "en", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AccountWithoutEmail_NeverCallsWelcomeBackEmail_ButStillSucceeds()
    {
        var user = User.Create(
            Guid.NewGuid(), "Ada", "+31612345678", null, UserRole.Passenger,
            isPhoneVerified: true, isEmailVerified: false).Value;

        var (handler, welcomeEmailService) = BuildHandler(new List<User> { user }, user.Id);

        var result = await handler.Handle(new ContinueExistingAccountCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await welcomeEmailService.DidNotReceiveWithAnyArgs()
            .SendWelcomeBackEmailAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task Handle_InvalidJwtId_ReturnsUserNotFound()
    {
        var context = Substitute.For<IAppDbContext>();
        var domainUsers = DbSetMockFactory.Create(new List<User>());
        context.DomainUsers.Returns(domainUsers);

        var currentUser = Substitute.For<IUser>();
        currentUser.Id.Returns("not-a-guid");

        var handler = new ContinueExistingAccountCommandHandler(context, currentUser, Substitute.For<IWelcomeEmailService>());

        var result = await handler.Handle(new ContinueExistingAccountCommand(), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(UserErrors.NotFound.Code, result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_NoMatchingActiveUser_ReturnsUserNotFound()
    {
        var (handler, _) = BuildHandler(new List<User>(), Guid.NewGuid());

        var result = await handler.Handle(new ContinueExistingAccountCommand(), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(UserErrors.NotFound.Code, result.Errors[0].Code);
    }

    private static (ContinueExistingAccountCommandHandler Handler, IWelcomeEmailService WelcomeEmailService) BuildHandler(
        List<User> existingUsers, Guid actingUserId)
    {
        var context = Substitute.For<IAppDbContext>();
        var domainUsers = DbSetMockFactory.Create(existingUsers);
        context.DomainUsers.Returns(domainUsers);

        var currentUser = Substitute.For<IUser>();
        currentUser.Id.Returns(actingUserId.ToString());

        var welcomeEmailService = Substitute.For<IWelcomeEmailService>();

        var handler = new ContinueExistingAccountCommandHandler(context, currentUser, welcomeEmailService);
        return (handler, welcomeEmailService);
    }
}
