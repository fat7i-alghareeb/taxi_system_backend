using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Trips.Commands.UpdatePassengerNote;

public class UpdatePassengerNoteCommandValidator : AbstractValidator<UpdatePassengerNoteCommand>
{
    public UpdatePassengerNoteCommandValidator()
    {
        RuleFor(v => v.TripId).NotEmpty();
        RuleFor(v => v.PassengerNote)
            .MaximumLength(500)
            .WithErrorCode(LocalizationKeys.Trip.PassengerNoteTooLong)
            .WithMessage(LocalizationKeys.Trip.PassengerNoteTooLong);
    }
}
