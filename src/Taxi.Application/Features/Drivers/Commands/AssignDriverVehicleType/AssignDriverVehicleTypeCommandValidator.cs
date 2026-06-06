using FluentValidation;

namespace Taxi.Application.Features.Drivers.Commands.AssignDriverVehicleType;

public class AssignDriverVehicleTypeCommandValidator : AbstractValidator<AssignDriverVehicleTypeCommand>
{
    public AssignDriverVehicleTypeCommandValidator()
    {
        RuleFor(c => c.DriverId)
            .NotEmpty();

        RuleFor(c => c.VehicleTypeId)
            .NotEmpty();
    }
}
