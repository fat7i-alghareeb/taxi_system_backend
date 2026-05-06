using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Trips.Commands.RequestTrip;

public class RequestTripCommandValidator : AbstractValidator<RequestTripCommand>
{
    public RequestTripCommandValidator()
    {
        RuleFor(v => v.PassengerId).NotEmpty();
        RuleFor(v => v.VehicleTypeId).NotEmpty();
        RuleFor(v => v.QuoteId).NotEmpty();
        RuleFor(v => v.Stops).NotEmpty().Must(s => s.Count >= 2);
    }
}
