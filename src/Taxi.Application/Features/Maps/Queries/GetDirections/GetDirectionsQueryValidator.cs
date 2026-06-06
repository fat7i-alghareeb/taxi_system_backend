using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Maps.Queries.GetDirections;

public class GetDirectionsQueryValidator : AbstractValidator<GetDirectionsQuery>
{
    public GetDirectionsQueryValidator()
    {
        RuleFor(q => q.Stops)
            .NotNull().WithErrorCode(LocalizationKeys.Maps.InsufficientStops)
            .Must(stops => stops.Count >= 2)
            .WithErrorCode(LocalizationKeys.Maps.InsufficientStops);

        RuleForEach(q => q.Stops).ChildRules(stop =>
        {
            stop.RuleFor(s => s.Latitude)
                .InclusiveBetween(-90m, 90m)
                .WithErrorCode(LocalizationKeys.Maps.CoordinateInvalid);

            stop.RuleFor(s => s.Longitude)
                .InclusiveBetween(-180m, 180m)
                .WithErrorCode(LocalizationKeys.Maps.CoordinateInvalid);
        });
    }
}
