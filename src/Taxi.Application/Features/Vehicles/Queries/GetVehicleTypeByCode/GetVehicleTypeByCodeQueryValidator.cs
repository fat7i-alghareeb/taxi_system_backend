using FluentValidation;

namespace Taxi.Application.Features.Vehicles.Queries.GetVehicleTypeByCode;

public class GetVehicleTypeByCodeQueryValidator : AbstractValidator<GetVehicleTypeByCodeQuery>
{
    public GetVehicleTypeByCodeQueryValidator()
    {
        RuleFor(v => v.Code)
            .NotEmpty();
    }
}

