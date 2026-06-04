using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Notifications.Commands.BroadcastNotification;

public class BroadcastNotificationCommandValidator : AbstractValidator<BroadcastNotificationCommand>
{
    public BroadcastNotificationCommandValidator()
    {
        RuleFor(c => c.Audience)
            .IsInEnum();

        RuleFor(c => c.Title)
            .NotEmpty().WithErrorCode(LocalizationKeys.Notification.TitleRequired);

        RuleFor(c => c.Body)
            .NotEmpty().WithErrorCode(LocalizationKeys.Notification.BodyRequired);
    }
}
