using MediatR;
using Taxi.Application.Features.Drivers.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Queries.GetCurrentDriverProfile;

public record GetCurrentDriverProfileQuery : IRequest<Result<DriverCurrentProfileDto>>;
