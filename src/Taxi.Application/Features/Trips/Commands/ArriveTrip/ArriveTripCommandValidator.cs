using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.ArriveTrip;

public class ArriveTripCommandValidator : AbstractValidator<ArriveTripCommand>
{
    public ArriveTripCommandValidator()
    {
        RuleFor(c => c.TripId)
            .NotEmpty();
    }
}
