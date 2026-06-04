using FluentValidation;

namespace Taxi.Application.Features.Drivers.Commands.ApproveDriver;

public class ApproveDriverCommandValidator : AbstractValidator<ApproveDriverCommand>
{
    public ApproveDriverCommandValidator()
    {
        RuleFor(c => c.DriverId)
            .NotEmpty();
    }
}
