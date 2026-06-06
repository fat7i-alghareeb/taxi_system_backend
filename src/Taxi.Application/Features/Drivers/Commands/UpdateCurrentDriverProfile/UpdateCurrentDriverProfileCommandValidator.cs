using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Drivers.Commands.UpdateCurrentDriverProfile;

public class UpdateCurrentDriverProfileCommandValidator : AbstractValidator<UpdateCurrentDriverProfileCommand>
{
    public UpdateCurrentDriverProfileCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithErrorCode(LocalizationKeys.User.ProfileNameRequired);

        When(c => !string.IsNullOrWhiteSpace(c.Email), () =>
        {
            RuleFor(c => c.Email!)
                .EmailAddress().WithErrorCode(LocalizationKeys.Validation.EmailInvalid);
        });
    }
}
