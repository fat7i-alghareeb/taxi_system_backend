using MediatR;
using Taxi.Application.Features.Vehicles.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Vehicles;

namespace Taxi.Application.Features.Vehicles.Commands.CreateVehicle;

public record CreateVehicleCommand(
    Guid VehicleTypeId,
    Guid DriverId,
    string Make,
    string Model,
    string Year,
    string Color,
    string LicensePlate) : IRequest<Result<VehicleDto>>;
