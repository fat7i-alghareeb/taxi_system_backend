using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.StopTripWaiting;

public class StopTripWaitingCommandValidator : AbstractValidator<StopTripWaitingCommand>
{
    public StopTripWaitingCommandValidator()
    {
        RuleFor(c => c.TripId)
            .NotEmpty();
    }
}
