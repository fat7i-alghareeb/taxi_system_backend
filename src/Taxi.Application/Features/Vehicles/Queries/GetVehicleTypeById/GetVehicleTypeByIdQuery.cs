using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Vehicles.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Queries.GetVehicleTypeById;

public sealed record GetVehicleTypeByIdQuery(Guid Id) : ICachedQuery<Result<VehicleTypeDto>>
{
    public string CacheKey => $"vehicle_type_id_{Id}";
    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
    public string[] Tags => ["vehicles"];
}
