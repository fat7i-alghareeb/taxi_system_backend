using MediatR;
using Taxi.Application.Features.Maps.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Maps.Queries.SearchPlaces;

public record SearchPlacesQuery(
    string Query,
    decimal? Latitude,
    decimal? Longitude) : IRequest<Result<List<PlaceResultDto>>>;

