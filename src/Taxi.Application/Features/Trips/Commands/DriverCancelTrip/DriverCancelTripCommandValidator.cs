using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Trips.Commands.DriverCancelTrip;

public class DriverCancelTripCommandValidator : AbstractValidator<DriverCancelTripCommand>
{
    public DriverCancelTripCommandValidator()
    {
        RuleFor(c => c.TripId)
            .NotEmpty();

        RuleFor(c => c.Reason)
            .NotEmpty().WithErrorCode(LocalizationKeys.Trip.InvalidCancellationReason);
    }
}
