using FluentValidation;

namespace Taxi.Application.Features.Users.Commands.SuspendPassenger;

public class SuspendPassengerCommandValidator : AbstractValidator<SuspendPassengerCommand>
{
    public SuspendPassengerCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Reason)
            .MaximumLength(500)
            .When(c => c.Reason is not null);
    }
}
