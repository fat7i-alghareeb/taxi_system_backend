using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Maps.Queries.ReverseGeocode;

public class ReverseGeocodeQueryValidator : AbstractValidator<ReverseGeocodeQuery>
{
    public ReverseGeocodeQueryValidator()
    {
        RuleFor(q => q.Latitude)
            .InclusiveBetween(-90m, 90m)
            .WithErrorCode(LocalizationKeys.Maps.CoordinateInvalid);

        RuleFor(q => q.Longitude)
            .InclusiveBetween(-180m, 180m)
            .WithErrorCode(LocalizationKeys.Maps.CoordinateInvalid);
    }
}
