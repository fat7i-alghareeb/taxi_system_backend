using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Uploads.Commands.UploadCompensationEvidence;

public class UploadCompensationEvidenceCommandValidator : AbstractValidator<UploadCompensationEvidenceCommand>
{
    public UploadCompensationEvidenceCommandValidator()
    {
        RuleFor(c => c.Files)
            .NotNull().WithErrorCode(LocalizationKeys.Validation.RequiredField)
            .Must(files => files.Count > 0)
            .WithErrorCode(LocalizationKeys.Validation.RequiredField);

        RuleForEach(c => c.Files).ChildRules(item =>
        {
            item.RuleFor(f => f.Stream)
                .NotNull().WithErrorCode(LocalizationKeys.Validation.RequiredField);

            item.RuleFor(f => f.FileName)
                .NotEmpty().WithErrorCode(LocalizationKeys.Validation.RequiredField);
        });
    }
}
