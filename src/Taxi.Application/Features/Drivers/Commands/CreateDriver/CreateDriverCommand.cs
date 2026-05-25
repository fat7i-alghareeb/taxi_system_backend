using MediatR;

using Taxi.Application.Features.Drivers.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Commands.CreateDriver;

public record CreateDriverCommand(
    string Phone,
    string Name,
    string LicenseNumber,
    Guid VehicleTypeId) : IRequest<Result<DriverDto>>;