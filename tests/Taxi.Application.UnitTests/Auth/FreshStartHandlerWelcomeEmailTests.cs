using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Commands.FreshStart;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

using Xunit;

namespace Taxi.Application.UnitTests.Auth;

public class FreshStartHandlerWelcomeEmailTests
{
    [Fact]
    public async Task Handle_EmailAccount_SendsWelcomeEmailToThePreResetAddress()
    {
        var user = User.Create(
            Guid.NewGuid(), "Ada", "+31612345678", "ada@example.com", UserRole.Passenger,
            isPhoneVerified: false, isEmailVerified: true).Value;

        var (handler, welcomeEmailService) = BuildHandler(user);

        var result = await handler.Handle(new FreshStartCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(user.Email); // reset already happened by the time we assert
        await welcomeEmailService.Received(1).SendWelcomeEmailAsync(
            "ada@example.com", null, "en", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PhoneOnlyAccount_NeverCallsWelcomeEmail()
    {
        var user = User.Create(
            Guid.NewGuid(), "Ada", "+31612345678", null, UserRole.Passenger,
            isPhoneVerified: true, isEmailVerified: false).Value;

        var (handler, welcomeEmailService) = BuildHandler(user);

        var result = await handler.Handle(new FreshStartCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await welcomeEmailService.DidNotReceiveWithAnyArgs()
            .SendWelcomeEmailAsync(default!, default, default!, default);
    }

    private static (FreshStartCommandHandler Handler, IWelcomeEmailService WelcomeEmailService) BuildHandler(User user)
    {
        var context = Substitute.For<IAppDbContext>();
        var domainUsers = DbSetMockFactory.Create(new List<User> { user });
        context.DomainUsers.Returns(domainUsers);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var currentUser = Substitute.For<IUser>();
        currentUser.Id.Returns(user.Id.ToString());

        var welcomeEmailService = Substitute.For<IWelcomeEmailService>();

        var sessionFactory = Substitute.For<IAuthSessionFactory>();
        sessionFactory
            .CreateAsync(Arg.Any<User>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult<Result<AuthResponse>>(BuildAuthResponse((User)ci[0])));

        var handler = new FreshStartCommandHandler(
            context, currentUser, sessionFactory, welcomeEmailService, TimeProvider.System);

        return (handler, welcomeEmailService);
    }

    private static AuthResponse BuildAuthResponse(User user) =>
        new("access", "refresh", new UserDto { Id = user.Id, Phone = user.Phone, Role = user.Role.ToString() });
}
