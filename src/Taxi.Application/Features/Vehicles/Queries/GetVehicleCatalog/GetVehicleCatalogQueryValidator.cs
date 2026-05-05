using FluentValidation;

namespace Taxi.Application.Features.Vehicles.Queries.GetVehicleCatalog;

public class GetVehicleCatalogQueryValidator : AbstractValidator<GetVehicleCatalogQuery>
{
    public GetVehicleCatalogQueryValidator()
    {
    }
}
