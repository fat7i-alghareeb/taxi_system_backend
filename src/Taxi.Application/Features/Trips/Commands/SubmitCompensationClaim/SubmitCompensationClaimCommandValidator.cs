using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Trips.Commands.SubmitCompensationClaim;

public class SubmitCompensationClaimCommandValidator : AbstractValidator<SubmitCompensationClaimCommand>
{
    public SubmitCompensationClaimCommandValidator()
    {
        RuleFor(c => c.TripId)
            .NotEmpty();

        RuleFor(c => c.Note)
            .NotEmpty().WithErrorCode(LocalizationKeys.Trip.CompensationClaimNoteRequired);
    }
}
