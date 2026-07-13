using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Commands.Google;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

using Xunit;

namespace Taxi.Application.UnitTests.Auth;

public class GoogleAuthHandlerWelcomeBackTests
{
    private const string GoogleUid = "google-uid-1";

    [Fact]
    public async Task Handle_ReturningUserMatchedByGoogleId_WithEmail_SendsWelcomeBackEmail()
    {
        var user = User.Create(
            Guid.NewGuid(), "Ada", "+31612345678", "ada@example.com", UserRole.Passenger,
            isPhoneVerified: false, isEmailVerified: true, googleId: GoogleUid).Value;

        var (handler, welcomeEmailService, _) = BuildHandler(new List<User> { user });

        var result = await handler.Handle(
            new GoogleAuthCommand("firebase-token", null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.RequiresRegistration);
        await welcomeEmailService.Received(1).SendWelcomeBackEmailAsync(
            "ada@example.com", user.Name, "en", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturningUserMatchedByGoogleId_WithNullEmail_DoesNotCallWelcomeBackEmail()
    {
        // Shape produced by "start fresh": GoogleId stays linked, Email/IsEmailVerified are
        // wiped. Regression test for the Part F fix (previously called with Email!, which
        // was actually null here).
        var user = User.Create(
            Guid.NewGuid(), "Passenger +31612345678", "+31612345678", null, UserRole.Passenger,
            isPhoneVerified: true, isEmailVerified: false, googleId: GoogleUid).Value;

        var (handler, welcomeEmailService, _) = BuildHandler(new List<User> { user });

        var result = await handler.Handle(
            new GoogleAuthCommand("firebase-token", null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await welcomeEmailService.DidNotReceiveWithAnyArgs()
            .SendWelcomeBackEmailAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task Handle_NoMatch_ReturnsRegistrationChallenge_AndNeverCallsWelcomeBackEmail()
    {
        var (handler, welcomeEmailService, registrationTokenService) = BuildHandler(new List<User>());
        registrationTokenService.Issue(Arg.Any<RegistrationTokenPayload>()).Returns("reg-token");

        var result = await handler.Handle(
            new GoogleAuthCommand("firebase-token", null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.RequiresRegistration);
        await welcomeEmailService.DidNotReceiveWithAnyArgs()
            .SendWelcomeBackEmailAsync(default!, default, default!, default);
    }

    private static (GoogleAuthCommandHandler Handler, IWelcomeEmailService WelcomeEmailService, IRegistrationTokenService RegistrationTokenService) BuildHandler(
        List<User> existingUsers)
    {
        var firebaseAuth = Substitute.For<IFirebaseAuthService>();
        firebaseAuth
            .VerifyIdTokenAndGetIdentityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<FirebaseIdentity>>(
                new FirebaseIdentity(GoogleUid, "ada@example.com", true, "Ada", null, "google.com")));

        var context = Substitute.For<IAppDbContext>();
        var domainUsers = DbSetMockFactory.Create(existingUsers);
        context.DomainUsers.Returns(domainUsers);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var registrationTokenService = Substitute.For<IRegistrationTokenService>();
        var welcomeEmailService = Substitute.For<IWelcomeEmailService>();

        var sessionFactory = Substitute.For<IAuthSessionFactory>();
        sessionFactory
            .CreateAsync(Arg.Any<User>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult<Result<AuthResponse>>(BuildAuthResponse((User)ci[0])));

        var handler = new GoogleAuthCommandHandler(
            firebaseAuth, context, registrationTokenService, welcomeEmailService, sessionFactory);

        return (handler, welcomeEmailService, registrationTokenService);
    }

    private static AuthResponse BuildAuthResponse(User user) =>
        new("access", "refresh", new UserDto { Id = user.Id, Phone = user.Phone, Role = user.Role.ToString() });
}
