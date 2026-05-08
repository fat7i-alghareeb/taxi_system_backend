using FluentValidation;

namespace Taxi.Application.Features.Vehicles.Commands.CreateVehicle;

public class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        RuleFor(v => v.VehicleTypeId).NotEmpty();
        RuleFor(v => v.DriverId).NotEmpty();
        RuleFor(v => v.Make).NotEmpty();
        RuleFor(v => v.Model).NotEmpty();
        RuleFor(v => v.Year).NotEmpty();
        RuleFor(v => v.LicensePlate).NotEmpty();
    }
}

