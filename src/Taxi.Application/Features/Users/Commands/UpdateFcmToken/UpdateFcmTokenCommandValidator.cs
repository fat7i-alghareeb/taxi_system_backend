using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Users.Commands.UpdateFcmToken;

public class UpdateFcmTokenCommandValidator : AbstractValidator<UpdateFcmTokenCommand>
{
    private const int MaxFcmTokenLength = 4096;

    public UpdateFcmTokenCommandValidator()
    {
        // The domain layer (User.UpdateFcmToken) tolerates null/empty (clears the token);
        // we only reject oversized tokens here to mirror that invariant.
        When(c => !string.IsNullOrEmpty(c.FcmToken), () =>
        {
            RuleFor(c => c.FcmToken)
                .MaximumLength(MaxFcmTokenLength)
                .WithErrorCode(LocalizationKeys.User.FcmTokenInvalid);
        });
    }
}
