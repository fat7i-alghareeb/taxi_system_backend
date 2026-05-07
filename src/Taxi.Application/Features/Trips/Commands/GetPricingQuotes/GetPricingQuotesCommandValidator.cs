using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Trips.Commands.GetPricingQuotes;

public class GetPricingQuotesCommandValidator : AbstractValidator<GetPricingQuotesCommand>
{
    public GetPricingQuotesCommandValidator()
    {
        RuleFor(v => v.Stops)
            .NotEmpty()
            .Must(s => s.Count >= 2)
            .WithMessage(LocalizationKeys.Trip.InvalidStops);

        RuleForEach(v => v.Stops).ChildRules(stop =>
        {
            stop.RuleFor(s => s.Latitude)
                .InclusiveBetween(-90, 90)
                .WithMessage(LocalizationKeys.Trip.InvalidCoordinate);

            stop.RuleFor(s => s.Longitude)
                .InclusiveBetween(-180, 180)
                .WithMessage(LocalizationKeys.Trip.InvalidCoordinate);
        });
    }
}
