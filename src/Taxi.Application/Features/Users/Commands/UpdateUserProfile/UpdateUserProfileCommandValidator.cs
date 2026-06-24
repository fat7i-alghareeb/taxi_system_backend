using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        // Profile fields are optional individually, but at least one update must be provided.
        RuleFor(c => c)
            .Must(c =>
                !string.IsNullOrWhiteSpace(c.Name)
                || c.Email is not null
                || c.PhotoStream is not null
                || c.HomeAddressOperation != HomeAddressUpdateMode.Keep
                || c.HomeAddressLabel is not null
                || c.HomeAddressLatitude is not null
                || c.HomeAddressLongitude is not null)
            .WithErrorCode(LocalizationKeys.Validation.RequiredField);

        When(c => c.HomeAddressOperation == HomeAddressUpdateMode.Set, () =>
        {
            RuleFor(c => c.HomeAddressLabel)
                .NotEmpty()
                .WithErrorCode(LocalizationKeys.Validation.RequiredField);

            RuleFor(c => c)
                .Must(c => c.HomeAddressLatitude.HasValue == c.HomeAddressLongitude.HasValue)
                .WithErrorCode(LocalizationKeys.User.HomeAddressInvalid);
        });

        When(c => !string.IsNullOrWhiteSpace(c.Name), () =>
        {
            RuleFor(c => c.Name!)
                .NotEmpty().WithErrorCode(LocalizationKeys.User.ProfileNameRequired);
        });

        RuleFor(c => c.Email)
            .MaximumLength(150)
            .EmailAddress()
            .WithErrorCode(LocalizationKeys.Validation.EmailInvalid)
            .When(c => !string.IsNullOrWhiteSpace(c.Email));

        When(c => c.PhotoStream is not null, () =>
        {
            RuleFor(c => c.PhotoContentType)
                .NotEmpty().WithErrorCode(LocalizationKeys.User.ProfilePhotoInvalid);
        });
    }
}
