using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Drivers.Commands.UpdateDriver;

public class UpdateDriverCommandValidator : AbstractValidator<UpdateDriverCommand>
{
    public UpdateDriverCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEmpty();

        RuleFor(c => c.LicenseNumber)
            .NotEmpty().WithErrorCode(LocalizationKeys.Driver.LicenseRequired);

        RuleFor(c => c.VehicleTypeId)
            .NotEmpty();
    }
}
