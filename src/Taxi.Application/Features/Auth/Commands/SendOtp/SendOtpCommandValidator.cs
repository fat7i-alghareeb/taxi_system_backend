using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Auth.Commands.SendOtp;

public sealed class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage(LocalizationKeys.User.PhoneRequired)
            .Matches(@"^\+?\d{7,15}$").WithMessage("Validation.PhoneNumber.Format");
    }
}

