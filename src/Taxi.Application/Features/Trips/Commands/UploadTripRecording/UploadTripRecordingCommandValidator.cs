using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Trips.Commands.UploadTripRecording;

public class UploadTripRecordingCommandValidator : AbstractValidator<UploadTripRecordingCommand>
{
    public UploadTripRecordingCommandValidator()
    {
        RuleFor(c => c.TripId)
            .NotEmpty().WithErrorCode(LocalizationKeys.Validation.RequiredField);

        RuleFor(c => c.File)
            .NotNull().WithErrorCode(LocalizationKeys.Validation.RequiredField);

        RuleFor(c => c.File.FileName)
            .NotEmpty().WithErrorCode(LocalizationKeys.Validation.RequiredField)
            .When(c => c.File is not null);

        RuleFor(c => c.DurationSeconds)
            .GreaterThan(0).WithErrorCode(LocalizationKeys.Validation.RequiredField)
            .When(c => c.DurationSeconds.HasValue);
    }
}
