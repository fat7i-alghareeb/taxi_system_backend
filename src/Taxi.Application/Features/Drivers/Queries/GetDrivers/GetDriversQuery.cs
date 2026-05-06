using MediatR;
using Taxi.Application.Features.Drivers.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Queries.GetDrivers;

public record GetDriversQuery : IRequest<Result<List<DriverDto>>>;
