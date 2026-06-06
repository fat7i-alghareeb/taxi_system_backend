using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Drivers.Commands.UploadDriverDocument;

public class UploadDriverDocumentCommandValidator : AbstractValidator<UploadDriverDocumentCommand>
{
    public UploadDriverDocumentCommandValidator()
    {
        RuleFor(c => c.DriverId)
            .NotEmpty().WithErrorCode(LocalizationKeys.DriverDocument.DriverIdRequired);

        RuleFor(c => c.Type)
            .IsInEnum();

        RuleFor(c => c.FileStream)
            .NotNull().WithErrorCode(LocalizationKeys.DriverDocument.FileUrlRequired);

        RuleFor(c => c.FileName)
            .NotEmpty().WithErrorCode(LocalizationKeys.DriverDocument.FileUrlRequired);
    }
}
