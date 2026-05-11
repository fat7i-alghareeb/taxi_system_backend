using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage(LocalizationKeys.User.PhoneRequired)
            .Matches(@"^\+?\d{7,15}$").WithMessage(LocalizationKeys.Validation.PhoneNumberFormat);

        RuleFor(x => x.FirebaseIdToken)
            .NotEmpty().WithMessage(LocalizationKeys.Auth.InvalidFirebaseToken)
            .MaximumLength(4096).WithMessage(LocalizationKeys.Auth.InvalidFirebaseToken);

        RuleFor(x => x.FcmToken)
            .MaximumLength(4096).WithMessage(LocalizationKeys.User.FcmTokenInvalid)
            .When(x => !string.IsNullOrWhiteSpace(x.FcmToken));
    }
}
