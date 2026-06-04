using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.StartTrip;

public class StartTripCommandValidator : AbstractValidator<StartTripCommand>
{
    public StartTripCommandValidator()
    {
        RuleFor(c => c.TripId)
            .NotEmpty();
    }
}
