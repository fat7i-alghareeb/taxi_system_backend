using Taxi.Application.Features.Auth.Commands.SendOtp;
using Taxi.Contracts.Common;

using Xunit;

namespace Taxi.Application.UnitTests.Auth;

public class SendOtpCommandValidatorTests
{
    [Fact]
    public void Validate_WhenPhoneEmpty_ReturnsRequiredError()
    {
        var validator = new SendOtpCommandValidator();

        var result = validator.Validate(new SendOtpCommand(string.Empty));

        Assert.Contains(result.Errors, e => e.ErrorMessage == LocalizationKeys.User.PhoneRequired);
    }

    [Fact]
    public void Validate_WhenPhoneInvalid_ReturnsFormatError()
    {
        var validator = new SendOtpCommandValidator();

        var result = validator.Validate(new SendOtpCommand("bad"));

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Validation.PhoneNumber.Format");
    }

    [Fact]
    public void Validate_WhenPhoneValid_ReturnsNoErrors()
    {
        var validator = new SendOtpCommandValidator();

        var result = validator.Validate(new SendOtpCommand("+1234567890"));

        Assert.Empty(result.Errors);
    }
}
