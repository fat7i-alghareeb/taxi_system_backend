using FluentValidation;

namespace Taxi.Application.Features.Vehicles.Commands.UpdateVehicle;

public class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
{
    public UpdateVehicleCommandValidator()
    {
        RuleFor(v => v.Id).NotEmpty();
        RuleFor(v => v.LicensePlate).NotEmpty();
    }
}
