using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.PreviewTripEdit;

public class PreviewTripEditCommandValidator : AbstractValidator<PreviewTripEditCommand>
{
    public PreviewTripEditCommandValidator()
    {
        RuleFor(c => c.TripId).NotEmpty();
        RuleFor(c => c)
            .Must(c => c.Stops is not null || c.PassengerCount is not null)
            .WithMessage("Either stops or a passenger count must be provided.");
        RuleFor(c => c.Stops!.Count).GreaterThanOrEqualTo(2).When(c => c.Stops is not null);
        RuleFor(c => c.PassengerCount!.Value).GreaterThanOrEqualTo(1).When(c => c.PassengerCount is not null);
    }
}
