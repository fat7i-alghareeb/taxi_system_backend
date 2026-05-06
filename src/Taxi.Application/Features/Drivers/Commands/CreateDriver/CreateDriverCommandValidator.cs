using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Drivers.Commands.CreateDriver;

public class CreateDriverCommandValidator : AbstractValidator<CreateDriverCommand>
{
    public CreateDriverCommandValidator()
    {
        RuleFor(v => v.Phone)
            .NotEmpty().WithMessage(LocalizationKeys.Validation.RequiredField);

        RuleFor(v => v.LicenseNumber)
            .NotEmpty().WithMessage(LocalizationKeys.Validation.RequiredField)
            .MaximumLength(50);
    }
}
