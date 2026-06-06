using FluentValidation;

namespace Taxi.Application.Features.Trips.Queries.GetTripReceipt;

public class GetTripReceiptQueryValidator : AbstractValidator<GetTripReceiptQuery>
{
    public GetTripReceiptQueryValidator()
    {
        RuleFor(q => q.TripId)
            .NotEmpty();
    }
}
