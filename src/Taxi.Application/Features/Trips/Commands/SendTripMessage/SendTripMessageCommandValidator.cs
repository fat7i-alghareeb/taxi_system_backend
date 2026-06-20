using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.SendTripMessage;

public sealed class SendTripMessageCommandValidator : AbstractValidator<SendTripMessageCommand>
{
    public SendTripMessageCommandValidator()
    {
        RuleFor(command => command.TripId).NotEmpty();
        RuleFor(command => command)
            .Must(command =>
                !string.IsNullOrWhiteSpace(command.Content) ||
                command.Photo is not null)
            .WithMessage("A text message or photo is required.");
        RuleFor(command => command.Content)
            .MaximumLength(2000);
    }
}
