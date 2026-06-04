using FluentValidation;

namespace Taxi.Application.Features.Drivers.Commands.SetDriverStatus;

public class SetDriverStatusCommandValidator : AbstractValidator<SetDriverStatusCommand>
{
    public SetDriverStatusCommandValidator()
    {
        RuleFor(c => c.Status)
            .IsInEnum();
    }
}
