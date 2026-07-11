using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.UpdateTripScheduledTime;

public class UpdateTripScheduledTimeCommandValidator : AbstractValidator<UpdateTripScheduledTimeCommand>
{
    public UpdateTripScheduledTimeCommandValidator()
    {
        RuleFor(c => c.TripId).NotEmpty();
    }
}
