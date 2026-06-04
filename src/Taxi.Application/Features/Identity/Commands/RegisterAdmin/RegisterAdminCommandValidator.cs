using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Identity.Commands.RegisterAdmin;

public class RegisterAdminCommandValidator : AbstractValidator<RegisterAdminCommand>
{
    public RegisterAdminCommandValidator()
    {
        RuleFor(c => c.UserName)
            .NotEmpty().WithErrorCode(LocalizationKeys.Validation.RequiredField);

        RuleFor(c => c.Password)
            .NotEmpty().WithErrorCode(LocalizationKeys.Validation.PasswordRequired)
            .MinimumLength(8).WithErrorCode(LocalizationKeys.Validation.PasswordMinLength);

        RuleFor(c => c.Name)
            .NotEmpty().WithErrorCode(LocalizationKeys.AdminProfile.NameRequired);

        RuleFor(c => c.Email)
            .NotEmpty().WithErrorCode(LocalizationKeys.AdminProfile.EmailRequired)
            .EmailAddress().WithErrorCode(LocalizationKeys.AdminProfile.EmailInvalid);

        When(c => !string.IsNullOrWhiteSpace(c.Phone1), () =>
        {
            RuleFor(c => c.Phone1!)
                .Matches(@"^\+?\d{7,15}$").WithErrorCode(LocalizationKeys.AdminProfile.PhoneInvalid);
        });

        When(c => !string.IsNullOrWhiteSpace(c.Phone2), () =>
        {
            RuleFor(c => c.Phone2!)
                .Matches(@"^\+?\d{7,15}$").WithErrorCode(LocalizationKeys.AdminProfile.PhoneInvalid);
        });
    }
}
