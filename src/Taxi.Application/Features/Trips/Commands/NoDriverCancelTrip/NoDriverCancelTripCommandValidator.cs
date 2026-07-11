using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.NoDriverCancelTrip;

public class NoDriverCancelTripCommandValidator : AbstractValidator<NoDriverCancelTripCommand>
{
    public NoDriverCancelTripCommandValidator()
    {
        RuleFor(c => c.TripId).NotEmpty();
    }
}
