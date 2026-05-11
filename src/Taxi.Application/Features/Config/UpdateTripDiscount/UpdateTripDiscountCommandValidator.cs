using FluentValidation;

namespace Taxi.Application.Features.Config.UpdateTripDiscount;

public class UpdateTripDiscountCommandValidator : AbstractValidator<UpdateTripDiscountCommand>
{
    public UpdateTripDiscountCommandValidator()
    {
        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(0, 100);
    }
}
