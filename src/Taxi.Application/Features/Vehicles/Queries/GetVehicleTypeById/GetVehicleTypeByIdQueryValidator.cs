using FluentValidation;

namespace Taxi.Application.Features.Vehicles.Queries.GetVehicleTypeById;

public class GetVehicleTypeByIdQueryValidator : AbstractValidator<GetVehicleTypeByIdQuery>
{
    public GetVehicleTypeByIdQueryValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty();
    }
}

