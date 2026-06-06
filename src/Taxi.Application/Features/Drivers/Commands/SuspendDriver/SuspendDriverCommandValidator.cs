using FluentValidation;

namespace Taxi.Application.Features.Drivers.Commands.SuspendDriver;

public class SuspendDriverCommandValidator : AbstractValidator<SuspendDriverCommand>
{
    public SuspendDriverCommandValidator()
    {
        RuleFor(c => c.DriverId)
            .NotEmpty();
    }
}
