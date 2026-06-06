using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Users.Commands.UploadProfilePhoto;

public class UploadProfilePhotoCommandValidator : AbstractValidator<UploadProfilePhotoCommand>
{
    public UploadProfilePhotoCommandValidator()
    {
        RuleFor(c => c.FileStream)
            .NotNull().WithErrorCode(LocalizationKeys.User.ProfilePhotoInvalid);

        RuleFor(c => c.FileName)
            .NotEmpty().WithErrorCode(LocalizationKeys.User.ProfilePhotoInvalid);

        RuleFor(c => c.ContentType)
            .NotEmpty().WithErrorCode(LocalizationKeys.User.ProfilePhotoInvalid);
    }
}
