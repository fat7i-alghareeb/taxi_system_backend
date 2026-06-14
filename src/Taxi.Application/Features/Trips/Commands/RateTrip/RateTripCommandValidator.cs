using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Trips.Commands.RateTrip;

public class RateTripCommandValidator : AbstractValidator<RateTripCommand>
{
    public RateTripCommandValidator()
    {
        RuleFor(v => v.TripId).NotEmpty();

        RuleFor(v => v.Stars)
            .InclusiveBetween(1, 5)
            .WithErrorCode(LocalizationKeys.Trip.InvalidRating)
            .WithMessage(LocalizationKeys.Trip.InvalidRating);

        RuleFor(v => v.Comment)
            .MaximumLength(500)
            .WithErrorCode(LocalizationKeys.Trip.PassengerNoteTooLong)
            .WithMessage(LocalizationKeys.Trip.PassengerNoteTooLong);
    }
}
