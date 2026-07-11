using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.UpdateTripStops;

public class UpdateTripStopsCommandValidator : AbstractValidator<UpdateTripStopsCommand>
{
    public UpdateTripStopsCommandValidator()
    {
        RuleFor(c => c.TripId).NotEmpty();
        RuleFor(c => c.Stops).NotNull();
        RuleFor(c => c.Stops.Count).GreaterThanOrEqualTo(2).When(c => c.Stops is not null);
    }
}
