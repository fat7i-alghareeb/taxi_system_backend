using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.ResendArrivedNotification;

public class ResendArrivedNotificationCommandValidator : AbstractValidator<ResendArrivedNotificationCommand>
{
    public ResendArrivedNotificationCommandValidator()
    {
        RuleFor(c => c.TripId)
            .NotEmpty();
    }
}
