using MediatR;
using Taxi.Application.Features.Vehicles.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Commands.UpdateVehicleType;

public record UpdateVehicleTypeCommand(
    Guid Id,
    decimal RatePerKm,
    decimal RatePerMin,
    decimal MinFare,
    bool IsActive) : IRequest<Result<VehicleTypeDto>>;
