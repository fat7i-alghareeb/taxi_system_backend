using FluentValidation;

namespace Taxi.Application.Features.Trips.Queries.GetTripInvoicePdf;

public class GetTripInvoicePdfQueryValidator : AbstractValidator<GetTripInvoicePdfQuery>
{
    public GetTripInvoicePdfQueryValidator()
    {
        RuleFor(q => q.TripId)
            .NotEmpty();
    }
}
