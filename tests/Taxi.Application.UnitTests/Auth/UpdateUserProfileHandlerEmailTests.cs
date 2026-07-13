using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Users.Commands.UpdateUserProfile;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Domain.Users;

using Xunit;

namespace Taxi.Application.UnitTests.Auth;

public class UpdateUserProfileHandlerEmailTests
{
    [Fact]
    public async Task Handle_LockedAccount_ChangingToDifferentEmail_ReturnsEmailChangeNotAllowed()
    {
        var user = CreateUser(email: "old@example.com", isEmailVerified: true);
        var (handler, context, welcomeEmailService) = BuildHandler(user);

        var result = await handler.Handle(
            new UpdateUserProfileCommand(Name: null, Email: "new@example.com", PhotoStream: null, PhotoContentType: null),
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(AuthErrors.EmailChangeNotAllowed.Code, result.Errors[0].Code);
        await welcomeEmailService.DidNotReceiveWithAnyArgs()
            .SendWelcomeEmailAsync(default!, default, default!, default);
        await context.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_LockedAccount_ResendingSameEmail_SucceedsWithoutWelcomeEmail()
    {
        var user = CreateUser(email: "same@example.com", isEmailVerified: true);
        var (handler, _, welcomeEmailService) = BuildHandler(user);

        var result = await handler.Handle(
            new UpdateUserProfileCommand(Name: null, Email: "SAME@example.com", PhotoStream: null, PhotoContentType: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await welcomeEmailService.DidNotReceiveWithAnyArgs()
            .SendWelcomeEmailAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task Handle_PhoneAccount_SettingNewEmailFromNull_SendsWelcomeEmail()
    {
        var user = CreateUser(email: null, isEmailVerified: false);
        var (handler, _, welcomeEmailService) = BuildHandler(user);

        var result = await handler.Handle(
            new UpdateUserProfileCommand(Name: null, Email: "new@example.com", PhotoStream: null, PhotoContentType: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await welcomeEmailService.Received(1).SendWelcomeEmailAsync(
            "new@example.com", user.Name, "en", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PhoneAccount_ChangingToADifferentEmail_SendsWelcomeEmailAgain()
    {
        var user = CreateUser(email: "old@example.com", isEmailVerified: false);
        var (handler, _, welcomeEmailService) = BuildHandler(user);

        var result = await handler.Handle(
            new UpdateUserProfileCommand(Name: null, Email: "new@example.com", PhotoStream: null, PhotoContentType: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await welcomeEmailService.Received(1).SendWelcomeEmailAsync(
            "new@example.com", user.Name, "en", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PhoneAccount_ResendingSameEmail_DoesNotSendWelcomeEmail()
    {
        var user = CreateUser(email: "same@example.com", isEmailVerified: false);
        var (handler, _, welcomeEmailService) = BuildHandler(user);

        var result = await handler.Handle(
            new UpdateUserProfileCommand(Name: null, Email: "same@example.com", PhotoStream: null, PhotoContentType: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await welcomeEmailService.DidNotReceiveWithAnyArgs()
            .SendWelcomeEmailAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task Handle_PhoneAccount_ClearingEmail_DoesNotSendWelcomeEmail()
    {
        var user = CreateUser(email: "old@example.com", isEmailVerified: false);
        var (handler, _, welcomeEmailService) = BuildHandler(user);

        var result = await handler.Handle(
            new UpdateUserProfileCommand(Name: null, Email: "", PhotoStream: null, PhotoContentType: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(user.Email);
        await welcomeEmailService.DidNotReceiveWithAnyArgs()
            .SendWelcomeEmailAsync(default!, default, default!, default);
    }

    private static User CreateUser(string? email, bool isEmailVerified) =>
        User.Create(
            Guid.NewGuid(), "Ada", "+31612345678", email, UserRole.Passenger,
            isPhoneVerified: true, isEmailVerified: isEmailVerified).Value;

    private static (UpdateUserProfileCommandHandler Handler, IAppDbContext Context, IWelcomeEmailService WelcomeEmailService) BuildHandler(User user)
    {
        var context = Substitute.For<IAppDbContext>();
        var domainUsers = DbSetMockFactory.Create(new List<User> { user });
        context.DomainUsers.Returns(domainUsers);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var currentUser = Substitute.For<IUser>();
        currentUser.Id.Returns(user.Id.ToString());

        var welcomeEmailService = Substitute.For<IWelcomeEmailService>();
        var fileStorage = Substitute.For<IFileStorage>();

        var handler = new UpdateUserProfileCommandHandler(context, currentUser, welcomeEmailService, fileStorage);
        return (handler, context, welcomeEmailService);
    }
}
