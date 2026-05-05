using FluentValidation;

namespace Taxi.Application.Features.Vehicles.Commands.UpdateVehicleType;

public class UpdateVehicleTypeCommandValidator : AbstractValidator<UpdateVehicleTypeCommand>
{
    public UpdateVehicleTypeCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty();

        RuleFor(v => v.RatePerKm)
            .GreaterThanOrEqualTo(0);

        RuleFor(v => v.RatePerMin)
            .GreaterThanOrEqualTo(0);

        RuleFor(v => v.MinFare)
            .GreaterThanOrEqualTo(0);
    }
}
