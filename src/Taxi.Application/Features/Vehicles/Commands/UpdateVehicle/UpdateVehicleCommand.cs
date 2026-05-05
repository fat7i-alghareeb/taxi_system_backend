using MediatR;
using Taxi.Application.Features.Vehicles.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Commands.UpdateVehicle;

public record UpdateVehicleCommand(
    Guid Id,
    string Color,
    string LicensePlate,
    bool IsActive) : IRequest<Result<VehicleDto>>;
