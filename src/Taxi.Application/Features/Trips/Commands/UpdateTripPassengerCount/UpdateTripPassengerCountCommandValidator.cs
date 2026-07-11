using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.UpdateTripPassengerCount;

public class UpdateTripPassengerCountCommandValidator : AbstractValidator<UpdateTripPassengerCountCommand>
{
    public UpdateTripPassengerCountCommandValidator()
    {
        RuleFor(c => c.TripId).NotEmpty();
        RuleFor(c => c.PassengerCount).GreaterThanOrEqualTo(1);
    }
}
