using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Admins.Commands.UpdateCurrentAdminProfile;

public class UpdateCurrentAdminProfileCommandValidator : AbstractValidator<UpdateCurrentAdminProfileCommand>
{
    public UpdateCurrentAdminProfileCommandValidator()
    {
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
