using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.AdminTakeTrip;

public class AdminTakeTripCommandValidator : AbstractValidator<AdminTakeTripCommand>
{
    public AdminTakeTripCommandValidator()
    {
        RuleFor(c => c.TripId)
            .NotEmpty();
    }
}
