using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Vehicles.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Queries.GetVehicleTypeByCode;

public sealed record GetVehicleTypeByCodeQuery(string Code) : ICachedQuery<Result<VehicleTypeDto>>
{
    public string CacheKey => $"vehicle_type_code_{Code}";
    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
    public string[] Tags => ["vehicles"];
}
