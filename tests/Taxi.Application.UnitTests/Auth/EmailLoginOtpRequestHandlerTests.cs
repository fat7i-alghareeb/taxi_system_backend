using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Commands.EmailLogin;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Contracts.Common;
using Taxi.Domain.Users;

using Xunit;

namespace Taxi.Application.UnitTests.Auth;

public class EmailLoginOtpRequestHandlerTests
{
    private const string Email = "ada@example.com";

    [Fact]
    public async Task Handle_WhenEmailNotVerified_ReturnsNoAccountFound_AndSendsNothing()
    {
        // An email stored as contact data (unverified) is not a login identity.
        var unverified = User.Create(
            Guid.NewGuid(), "Ada", "+31612345678", Email, UserRole.Passenger, isEmailVerified: false).Value;

        var context = Substitute.For<IAppDbContext>();
        var users = DbSetMockFactory.Create(new List<User> { unverified });
        context.DomainUsers.Returns(users);

        var otpService = Substitute.For<IOtpService>();
        var handler = new RequestEmailLoginOtpCommandHandler(context, otpService);

        var result = await handler.Handle(new RequestEmailLoginOtpCommand(Email, null), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(LocalizationKeys.Auth.NoAccountFound, result.Error.Code);
        await otpService.DidNotReceiveWithAnyArgs().IssueAsync(default, default, default!, default, default);
    }
}
