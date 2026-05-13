using MediatR;
using Taxi.Contracts.Responses.Vehicles;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Commands.UpdateVehicleType;

public record UpdateVehicleTypeCommand(
    Guid Id,
    decimal RatePerKm,
    decimal RatePerMin,
    decimal MinFare,
    bool IsActive,
    int SortOrder) : IRequest<Result<VehicleTypeDto>>;

