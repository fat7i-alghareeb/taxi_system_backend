using FluentValidation;

namespace Taxi.Application.Features.Trips.Queries.GetTripInvoice;

public class GetTripInvoiceQueryValidator : AbstractValidator<GetTripInvoiceQuery>
{
    public GetTripInvoiceQueryValidator()
    {
        RuleFor(q => q.TripId)
            .NotEmpty();
    }
}
