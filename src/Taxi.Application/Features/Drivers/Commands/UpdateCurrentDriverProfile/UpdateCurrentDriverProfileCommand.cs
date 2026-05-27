using MediatR;
using Taxi.Application.Features.Drivers.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Commands.UpdateCurrentDriverProfile;

public record UpdateCurrentDriverProfileCommand(
    string Name,
    string? Email) : IRequest<Result<DriverCurrentProfileDto>>;
