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
        RuleFor(v => v.PassengerNote)
            .MaximumLength(500)
            .WithErrorCode(LocalizationKeys.Trip.PassengerNoteTooLong)
            .WithMessage(LocalizationKeys.Trip.PassengerNoteTooLong);
        RuleFor(v => v.FlightNumber)
            .NotEmpty()
            .WithErrorCode(LocalizationKeys.Trip.FlightNumberRequired)
            .WithMessage(LocalizationKeys.Trip.FlightNumberRequired)
            .When(v => v.Stops.FirstOrDefault()?.IsAirport == true);
        RuleFor(v => v.FlightNumber)
            .Must(BeValidFlightNumber)
            .WithErrorCode(LocalizationKeys.Trip.FlightNumberInvalid)
            .WithMessage(LocalizationKeys.Trip.FlightNumberInvalid)
            .When(v => v.Stops.FirstOrDefault()?.IsAirport == true
                && !string.IsNullOrWhiteSpace(v.FlightNumber));
    }

    private static bool BeValidFlightNumber(string? value)
    {
        var normalized = System.Text.RegularExpressions.Regex.Replace(
            value!.Trim().ToUpperInvariant(),
            @"\s+",
            " ");
        return normalized.Length is >= 2 and <= 15
            && System.Text.RegularExpressions.Regex.IsMatch(
                normalized,
                @"^[A-Z0-9](?:[A-Z0-9 -]{0,13}[A-Z0-9])?$");
    }
}

