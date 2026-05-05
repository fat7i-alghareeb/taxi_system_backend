using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Vehicles.Commands.CreateVehicleType;

public class CreateVehicleTypeCommandValidator : AbstractValidator<CreateVehicleTypeCommand>
{
    public CreateVehicleTypeCommandValidator()
    {
        RuleFor(v => v.Code)
            .NotEmpty().WithErrorCode(LocalizationKeys.Vehicle.CodeRequired);

        RuleFor(v => v.NameEn)
            .NotEmpty().WithErrorCode(LocalizationKeys.Vehicle.NameEnRequired);

        RuleFor(v => v.NameAr)
            .NotEmpty().WithErrorCode(LocalizationKeys.Vehicle.NameArRequired);

        RuleFor(v => v.NameNl)
            .NotEmpty().WithErrorCode(LocalizationKeys.Vehicle.NameNlRequired);

        RuleFor(v => v.Capacity)
            .GreaterThan(0);

        RuleFor(v => v.RatePerKm)
            .GreaterThanOrEqualTo(0);

        RuleFor(v => v.RatePerMin)
            .GreaterThanOrEqualTo(0);

        RuleFor(v => v.MinFare)
            .GreaterThanOrEqualTo(0);
    }
}
