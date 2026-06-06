using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Drivers.Commands.ReviewDriverDocument;

public class ReviewDriverDocumentCommandValidator : AbstractValidator<ReviewDriverDocumentCommand>
{
    public ReviewDriverDocumentCommandValidator()
    {
        RuleFor(c => c.DriverId)
            .NotEmpty();

        RuleFor(c => c.DocumentId)
            .NotEmpty();

        // If the reviewer rejects, notes are mandatory (mirrors DriverDocument.Reject domain rule).
        When(c => !c.Approved, () =>
        {
            RuleFor(c => c.Notes)
                .NotEmpty().WithErrorCode(LocalizationKeys.DriverDocument.RejectionNotesRequired);
        });
    }
}
