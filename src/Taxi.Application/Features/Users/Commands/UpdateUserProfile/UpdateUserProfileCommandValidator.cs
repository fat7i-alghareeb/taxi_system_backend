using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        // Both Name and Photo are optional individually, but at least one must be provided.
        RuleFor(c => c)
            .Must(c => !string.IsNullOrWhiteSpace(c.Name) || c.PhotoStream is not null)
            .WithErrorCode(LocalizationKeys.Validation.RequiredField);

        When(c => !string.IsNullOrWhiteSpace(c.Name), () =>
        {
            RuleFor(c => c.Name!)
                .NotEmpty().WithErrorCode(LocalizationKeys.User.ProfileNameRequired);
        });

        When(c => c.PhotoStream is not null, () =>
        {
            RuleFor(c => c.PhotoContentType)
                .NotEmpty().WithErrorCode(LocalizationKeys.User.ProfilePhotoInvalid);
        });
    }
}
