using FluentValidation;

namespace Taxi.Application.Features.Vehicles.Commands.RemoveVehicleType;

public class RemoveVehicleTypeCommandValidator : AbstractValidator<RemoveVehicleTypeCommand>
{
    public RemoveVehicleTypeCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty();
    }
}

