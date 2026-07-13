using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Commands.EmailLogin;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Auth;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

using Xunit;

namespace Taxi.Application.UnitTests.Auth;

public class VerifyEmailLoginOtpHandlerWelcomeBackTests
{
    private const string Email = "ada@example.com";

    [Fact]
    public async Task Handle_ExistingVerifiedEmailAccount_SendsWelcomeBackEmail()
    {
        var user = User.Create(
            Guid.NewGuid(), "Ada", "+31612345678", Email, UserRole.Passenger,
            isPhoneVerified: true, isEmailVerified: true).Value;

        var context = Substitute.For<IAppDbContext>();
        var domainUsers = DbSetMockFactory.Create(new List<User> { user });
        context.DomainUsers.Returns(domainUsers);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var otpService = Substitute.For<IOtpService>();
        otpService
            .VerifyAsync(Arg.Any<Guid>(), Arg.Any<string>(), OtpChannel.Email, OtpPurpose.EmailLogin, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<string>>(Email));

        var welcomeEmailService = Substitute.For<IWelcomeEmailService>();

        var sessionFactory = Substitute.For<IAuthSessionFactory>();
        sessionFactory
            .CreateAsync(Arg.Any<User>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult<Result<AuthResponse>>(BuildAuthResponse((User)ci[0])));

        var handler = new VerifyEmailLoginOtpCommandHandler(context, otpService, welcomeEmailService, sessionFactory);

        var result = await handler.Handle(
            new VerifyEmailLoginOtpCommand(Guid.NewGuid(), "123456", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await welcomeEmailService.Received(1).SendWelcomeBackEmailAsync(
            Email, user.Name, "en", Arg.Any<CancellationToken>());
    }

    private static AuthResponse BuildAuthResponse(User user) =>
        new("access", "refresh", new UserDto { Id = user.Id, Phone = user.Phone, Role = user.Role.ToString() });
}
