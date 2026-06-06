using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.StartTripWaiting;

public class StartTripWaitingCommandValidator : AbstractValidator<StartTripWaitingCommand>
{
    public StartTripWaitingCommandValidator()
    {
        RuleFor(c => c.TripId)
            .NotEmpty();
    }
}
