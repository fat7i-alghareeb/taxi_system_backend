using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Trips.Queries.GetAllTrips;

public class GetAllTripsQueryValidator : AbstractValidator<GetAllTripsQuery>
{
    public GetAllTripsQueryValidator()
    {
        RuleFor(q => q.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode(LocalizationKeys.Validation.PageInvalid);

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100)
            .WithErrorCode(LocalizationKeys.Validation.PageSizeInvalid);
    }
}
