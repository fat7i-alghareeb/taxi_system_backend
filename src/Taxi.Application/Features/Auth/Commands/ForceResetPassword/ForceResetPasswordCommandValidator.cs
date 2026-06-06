using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Auth.Commands.ForceResetPassword;

public class ForceResetPasswordCommandValidator : AbstractValidator<ForceResetPasswordCommand>
{
    public ForceResetPasswordCommandValidator()
    {
        RuleFor(c => c.NewPassword)
            .NotEmpty().WithErrorCode(LocalizationKeys.Validation.PasswordRequired)
            .MinimumLength(8).WithErrorCode(LocalizationKeys.Validation.PasswordMinLength);
    }
}
