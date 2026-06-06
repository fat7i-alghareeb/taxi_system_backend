using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.CancelTrip;

public class CancelTripCommandValidator : AbstractValidator<CancelTripCommand>
{
    public CancelTripCommandValidator()
    {
        RuleFor(c => c.TripId)
            .NotEmpty();
    }
}
