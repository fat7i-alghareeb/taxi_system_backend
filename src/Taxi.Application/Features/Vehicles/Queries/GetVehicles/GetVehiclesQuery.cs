using MediatR;
using Taxi.Application.Features.Vehicles.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Queries.GetVehicles;

public record GetVehiclesQuery : IRequest<Result<List<VehicleDto>>>;
