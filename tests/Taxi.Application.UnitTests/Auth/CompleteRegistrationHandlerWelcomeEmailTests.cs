using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Commands.CompleteRegistration;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

using Xunit;

namespace Taxi.Application.UnitTests.Auth;

public class CompleteRegistrationHandlerWelcomeEmailTests
{
    [Fact]
    public async Task Handle_NewAccount_SendsWelcomeEmailWithEnteredNameAndEmail()
    {
        var registrationTokenService = Substitute.For<IRegistrationTokenService>();
        registrationTokenService.Validate(Arg.Any<string>())
            .Returns(new RegistrationTokenPayload("ada@example.com", "Old Name", null, EmailVerified: true));

        var context = Substitute.For<IAppDbContext>();
        var domainUsers = DbSetMockFactory.Create(new List<User>());
        context.DomainUsers.Returns(domainUsers);

        var identityService = Substitute.For<IIdentityService>();
        identityService.CreatePasswordlessUserAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>())
            .Returns(Task.FromResult<Result<string>>(Guid.NewGuid().ToString()));

        var welcomeEmailService = Substitute.For<IWelcomeEmailService>();

        var sessionFactory = Substitute.For<IAuthSessionFactory>();
        sessionFactory
            .CreateAsync(Arg.Any<User>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult<Result<AuthResponse>>(BuildAuthResponse((User)ci[0])));

        var handler = new CompleteRegistrationCommandHandler(
            registrationTokenService, context, identityService, welcomeEmailService, sessionFactory);

        var result = await handler.Handle(
            new CompleteRegistrationCommand("token", "Ada Lovelace", "+31612345678", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await welcomeEmailService.Received(1).SendWelcomeEmailAsync(
            "ada@example.com", "Ada Lovelace", "en", Arg.Any<CancellationToken>());
    }

    private static AuthResponse BuildAuthResponse(User user) =>
        new("access", "refresh", new UserDto { Id = user.Id, Phone = user.Phone, Role = user.Role.ToString() });
}
