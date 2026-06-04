using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Users.Commands.UpdatePreferredLanguage;

public class UpdatePreferredLanguageCommandValidator : AbstractValidator<UpdatePreferredLanguageCommand>
{
    private static readonly string[] Supported =
    {
        "en", "ar", "nl", "de", "pl", "uk", "fr", "es", "ro",
    };

    public UpdatePreferredLanguageCommandValidator()
    {
        RuleFor(c => c.LanguageCode)
            .NotEmpty().WithErrorCode(LocalizationKeys.User.PreferredLanguageRequired);

        RuleFor(c => c.LanguageCode)
            .Must(code => code is not null && Supported.Contains(code.Trim().ToLowerInvariant()))
            .WithErrorCode(LocalizationKeys.User.PreferredLanguageInvalid)
            .When(c => !string.IsNullOrWhiteSpace(c.LanguageCode));
    }
}
