using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.UpdateTripBagCount;

public class UpdateTripBagCountCommandValidator : AbstractValidator<UpdateTripBagCountCommand>
{
    public UpdateTripBagCountCommandValidator()
    {
        RuleFor(c => c.TripId).NotEmpty();
        RuleFor(c => c.BagCount).GreaterThanOrEqualTo(0);
    }
}
