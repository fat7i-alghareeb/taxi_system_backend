using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Auth.Commands.VerifyOtp;

public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage(LocalizationKeys.User.PhoneRequired)
            .Matches(@"^\+?\d{7,15}$").WithMessage("Validation.PhoneNumber.Format");

        RuleFor(x => x.SessionToken)
            .NotEmpty().WithMessage(LocalizationKeys.Auth.SessionNotFound);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(LocalizationKeys.Auth.InvalidOtp)
            .Length(4).WithMessage(LocalizationKeys.Auth.InvalidOtp)
            .Matches(@"^\d+$").WithMessage(LocalizationKeys.Auth.InvalidOtp);
    }
}

