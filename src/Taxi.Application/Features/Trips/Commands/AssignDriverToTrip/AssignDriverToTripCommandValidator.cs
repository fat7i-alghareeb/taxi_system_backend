using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.AssignDriverToTrip;

public class AssignDriverToTripCommandValidator : AbstractValidator<AssignDriverToTripCommand>
{
    public AssignDriverToTripCommandValidator()
    {
        RuleFor(c => c.TripId)
            .NotEmpty();

        RuleFor(c => c.DriverId)
            .NotEmpty();
    }
}
