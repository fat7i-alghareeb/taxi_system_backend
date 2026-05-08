using MediatR;
using Taxi.Contracts.Responses.Vehicles;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Commands.UpdateVehicle;

public record UpdateVehicleCommand(
    Guid Id,
    string Color,
    string LicensePlate,
    bool IsActive) : IRequest<Result<VehicleDto>>;

