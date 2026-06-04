using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.CompleteTrip;

public class CompleteTripCommandValidator : AbstractValidator<CompleteTripCommand>
{
    public CompleteTripCommandValidator()
    {
        RuleFor(c => c.TripId)
            .NotEmpty();
    }
}
