using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Maps.Queries.SearchPlaces;

public class SearchPlacesQueryValidator : AbstractValidator<SearchPlacesQuery>
{
    public SearchPlacesQueryValidator()
    {
        RuleFor(q => q.Query)
            .NotEmpty().WithErrorCode(LocalizationKeys.Maps.QueryRequired);

        // Latitude/Longitude are optional bias hints. If supplied, both must be valid.
        When(q => q.Latitude.HasValue, () =>
        {
            RuleFor(q => q.Latitude!.Value)
                .InclusiveBetween(-90m, 90m)
                .WithErrorCode(LocalizationKeys.Maps.CoordinateInvalid);
        });

        When(q => q.Longitude.HasValue, () =>
        {
            RuleFor(q => q.Longitude!.Value)
                .InclusiveBetween(-180m, 180m)
                .WithErrorCode(LocalizationKeys.Maps.CoordinateInvalid);
        });
    }
}
