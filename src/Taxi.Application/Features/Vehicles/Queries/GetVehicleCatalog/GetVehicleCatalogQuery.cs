using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Vehicles;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Queries.GetVehicleCatalog;

public record GetVehicleCatalogQuery : ICachedQuery<Result<List<VehicleTypeDto>>>
{
    public string CacheKey => "vehicle-catalog";
    public string[] Tags => ["vehicles"];
    public TimeSpan Expiration => TimeSpan.FromMinutes(30);
    public bool IsCultureAware => true;
}

