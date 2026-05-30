using MediatR;
using Taxi.Contracts.Responses.Vehicles;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Queries.GetAllVehicleTypes;

/// <summary>
/// Admin-only listing of every vehicle type, including deactivated ones.
/// Unlike <c>GetVehicleCatalogQuery</c> this is not cached and does not filter
/// by <c>IsActive</c>, so administrators can see and manage disabled types.
/// </summary>
public record GetAllVehicleTypesQuery : IRequest<Result<List<VehicleTypeDto>>>;
