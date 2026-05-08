using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Vehicles;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Queries.GetVehicleTypeByCode;

public sealed record GetVehicleTypeByCodeQuery(string Code) : ICachedQuery<Result<VehicleTypeDto>>
{
    public string CacheKey => $"vehicle_type_code_{Code}";
    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
    public string[] Tags => ["vehicles"];
}

