using FluentValidation;

namespace Taxi.Application.Features.Trips.Queries.GetTripById;

public class GetTripByIdQueryValidator : AbstractValidator<GetTripByIdQuery>
{
    public GetTripByIdQueryValidator()
    {
        RuleFor(q => q.Id)
            .NotEmpty();
    }
}
