using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Infrastructure.Email;
using Taxi.Infrastructure.Settings;

using Xunit;

namespace Taxi.Application.UnitTests.Auth;

public class WelcomeEmailServiceTests
{
    [Fact]
    public async Task SendWelcomeEmailAsync_WhenEmailBlank_NeverCallsEmailSender()
    {
        var emailSender = Substitute.For<IEmailSender>();
        var service = BuildService(emailSender, out _);

        await service.SendWelcomeEmailAsync("   ", "Ada", "en");

        await emailSender.DidNotReceiveWithAnyArgs()
            .SendAsync(default!, default!, default!, default, default);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WhenEmailSenderThrows_DoesNotThrow()
    {
        var emailSender = Substitute.For<IEmailSender>();
        emailSender
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<Task<Result<Success>>>(_ => throw new InvalidOperationException("SMTP down"));

        var service = BuildService(emailSender, out _);

        var exception = await Record.ExceptionAsync(
            () => service.SendWelcomeEmailAsync("ada@example.com", "Ada", "en"));

        Assert.Null(exception);
    }

    private static WelcomeEmailService BuildService(IEmailSender emailSender, out IStringLocalizer localizer)
    {
        localizer = Substitute.For<IStringLocalizer>();
        localizer[Arg.Any<string>()].Returns(ci => new LocalizedString(ci.Arg<string>(), "localized value"));

        var localizerFactory = Substitute.For<IStringLocalizerFactory>();
        localizerFactory.Create("Taxi.Api.SharedResource", "Taxi.Api").Returns(localizer);

        var options = Options.Create(new TitanEmailOptions());
        var logger = Substitute.For<ILogger<WelcomeEmailService>>();

        return new WelcomeEmailService(emailSender, localizerFactory, options, logger);
    }
}
