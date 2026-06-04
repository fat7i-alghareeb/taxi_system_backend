using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Admins.Commands.ChangeAdminPassword;

public class ChangeAdminPasswordCommandValidator : AbstractValidator<ChangeAdminPasswordCommand>
{
    public ChangeAdminPasswordCommandValidator()
    {
        RuleFor(c => c.CurrentPassword)
            .NotEmpty().WithErrorCode(LocalizationKeys.Validation.PasswordRequired);

        RuleFor(c => c.NewPassword)
            .NotEmpty().WithErrorCode(LocalizationKeys.Validation.PasswordRequired)
            .MinimumLength(8).WithErrorCode(LocalizationKeys.Validation.PasswordMinLength);
    }
}
