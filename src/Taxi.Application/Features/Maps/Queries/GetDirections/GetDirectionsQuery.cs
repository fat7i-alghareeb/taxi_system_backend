using MediatR;
using Taxi.Application.Features.Maps.Dtos;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Maps.Queries.GetDirections;

public record GetDirectionsQuery(List<CoordinateDto> Stops) : IRequest<Result<DirectionsDto>>;
