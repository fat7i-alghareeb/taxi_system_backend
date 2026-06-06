using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Trips.Commands.ReviewCompensationClaim;

public class ReviewCompensationClaimCommandValidator : AbstractValidator<ReviewCompensationClaimCommand>
{
    public ReviewCompensationClaimCommandValidator()
    {
        RuleFor(c => c.ClaimId)
            .NotEmpty();

        // If the reviewer rejects, notes are required to explain why.
        When(c => !c.Approved, () =>
        {
            RuleFor(c => c.Notes)
                .NotEmpty().WithErrorCode(LocalizationKeys.Trip.CompensationClaimNoteRequired);
        });
    }
}
