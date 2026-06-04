using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.CompleteStop;

public class CompleteStopCommandValidator : AbstractValidator<CompleteStopCommand>
{
    public CompleteStopCommandValidator()
    {
        RuleFor(c => c.TripId)
            .NotEmpty();

        RuleFor(c => c.Sequence)
            .GreaterThanOrEqualTo(0);
    }
}
