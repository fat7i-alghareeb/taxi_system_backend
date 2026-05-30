using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Trips.Commands.RequestTrip;

public class RequestTripCommandValidator : AbstractValidator<RequestTripCommand>
{
    public RequestTripCommandValidator()
    {
        RuleFor(v => v.QuoteId).NotEmpty();
        RuleFor(v => v.Stops)
            .NotEmpty()
            .Must(s => s.Count >= 2)
            .WithMessage(LocalizationKeys.Trip.InvalidStops);
        RuleFor(v => v.Stops)
            .Must(s => s.Count <= 25)
            .WithErrorCode(LocalizationKeys.Trip.TooManyStops)
            .WithMessage(LocalizationKeys.Trip.TooManyStops);
        RuleFor(v => v.ScheduledAt)
            .Must(s => s == null || s > DateTimeOffset.UtcNow.AddMinutes(15))
            .WithErrorCode(LocalizationKeys.Trip.ScheduledAtTooSoon)
            .When(v => v.ScheduledAt.HasValue);
    }
}

