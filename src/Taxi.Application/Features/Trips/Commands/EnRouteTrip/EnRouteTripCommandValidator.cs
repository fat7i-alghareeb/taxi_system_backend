using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.EnRouteTrip;

public class EnRouteTripCommandValidator : AbstractValidator<EnRouteTripCommand>
{
    public EnRouteTripCommandValidator()
    {
        RuleFor(c => c.TripId)
            .NotEmpty();
    }
}
