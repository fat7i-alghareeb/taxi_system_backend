using FluentValidation;

namespace Taxi.Application.Features.Trips.Queries.GetTripDetails;

public class GetTripDetailsQueryValidator : AbstractValidator<GetTripDetailsQuery>
{
    public GetTripDetailsQueryValidator()
    {
        RuleFor(q => q.TripId)
            .NotEmpty();
    }
}
