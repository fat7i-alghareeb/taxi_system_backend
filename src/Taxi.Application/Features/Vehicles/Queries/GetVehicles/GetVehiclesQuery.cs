using MediatR;
using Taxi.Contracts.Responses.Vehicles;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Queries.GetVehicles;

public record GetVehiclesQuery : IRequest<Result<List<VehicleDto>>>;

