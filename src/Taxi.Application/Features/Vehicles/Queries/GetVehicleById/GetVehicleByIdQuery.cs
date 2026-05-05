using MediatR;
using Taxi.Application.Features.Vehicles.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Queries.GetVehicleById;

public record GetVehicleByIdQuery(Guid Id) : IRequest<Result<VehicleDto>>;
