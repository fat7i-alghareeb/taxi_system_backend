using MediatR;
using Taxi.Application.Features.Drivers.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Queries.GetDriverById;

public record GetDriverByIdQuery(Guid Id) : IRequest<Result<DriverDto>>;

