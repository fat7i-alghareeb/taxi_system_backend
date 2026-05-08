using MediatR;
using Taxi.Application.Features.Drivers.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Commands.UpdateDriver;

public record UpdateDriverCommand(
    Guid Id,
    string LicenseNumber) : IRequest<Result<DriverDto>>;

