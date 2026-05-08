using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Users.Commands.UpdateProfile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithErrorCode(LocalizationKeys.User.ProfileNameRequired)
            .MaximumLength(100).WithErrorCode(LocalizationKeys.Validation.InvalidFormat);
    }
}

