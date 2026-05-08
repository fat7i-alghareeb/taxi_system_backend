using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Commands.AssignVehicle;

public record AssignVehicleCommand(Guid DriverId, Guid VehicleId) : IRequest<Result<Success>>;

