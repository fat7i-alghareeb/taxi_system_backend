using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Commands.PhoneLogin;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Contracts.Common;
using Taxi.Domain.Auth;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

using Xunit;

namespace Taxi.Application.UnitTests.Auth;

public class PhoneLoginOtpRequestHandlerTests
{
    private const string Phone = "+31612345678";

    [Fact]
    public async Task Handle_WhenNoVerifiedAccount_ReturnsNoAccountFound_AndSendsNothing()
    {
        var context = Substitute.For<IAppDbContext>();
        var users = DbSetMockFactory.Create(new List<User>());
        context.DomainUsers.Returns(users);

        var otpService = Substitute.For<IOtpService>();
        var handler = new RequestPhoneLoginOtpCommandHandler(context, otpService);

        var result = await handler.Handle(new RequestPhoneLoginOtpCommand(Phone, null), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(LocalizationKeys.Auth.NoAccountFound, result.Error.Code);
        await otpService.DidNotReceiveWithAnyArgs().IssueAsync(default, default, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenUnverifiedAccount_ReturnsNoAccountFound()
    {
        // A phone entered but never verified (Google/email skip) is contact data, not a login identity.
        var unverified = User.Create(Guid.NewGuid(), "Ada", Phone, null, UserRole.Passenger, isPhoneVerified: false).Value;
        var context = Substitute.For<IAppDbContext>();
        var users = DbSetMockFactory.Create(new List<User> { unverified });
        context.DomainUsers.Returns(users);

        var otpService = Substitute.For<IOtpService>();
        var handler = new RequestPhoneLoginOtpCommandHandler(context, otpService);

        var result = await handler.Handle(new RequestPhoneLoginOtpCommand(Phone, null), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(LocalizationKeys.Auth.NoAccountFound, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenVerifiedAccount_IssuesOtp()
    {
        var verified = User.Create(Guid.NewGuid(), "Ada", Phone, null, UserRole.Passenger, isPhoneVerified: true).Value;
        var context = Substitute.For<IAppDbContext>();
        var users = DbSetMockFactory.Create(new List<User> { verified });
        context.DomainUsers.Returns(users);

        var otpService = Substitute.For<IOtpService>();
        Result<OtpIssueResult> issued = new OtpIssueResult(Guid.NewGuid(), 300, 60);
        otpService.IssueAsync(OtpChannel.Sms, OtpPurpose.PhoneLogin, Phone, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(issued);

        var handler = new RequestPhoneLoginOtpCommandHandler(context, otpService);

        var result = await handler.Handle(new RequestPhoneLoginOtpCommand(Phone, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(issued.Value.OtpRequestId, result.Value.OtpRequestId);
    }
}
