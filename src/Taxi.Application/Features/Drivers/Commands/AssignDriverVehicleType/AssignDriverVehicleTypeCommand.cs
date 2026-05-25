using MediatR;

using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Commands.AssignDriverVehicleType;

public record AssignDriverVehicleTypeCommand(Guid DriverId, Guid VehicleTypeId) : IRequest<Result<Success>>;