using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Trips.Queries.GetPassengerTrips;

public class GetPassengerTripsQueryValidator : AbstractValidator<GetPassengerTripsQuery>
{
    public GetPassengerTripsQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode(LocalizationKeys.Validation.PageInvalid);

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100)
            .WithErrorCode(LocalizationKeys.Validation.PageSizeInvalid);
    }
}
