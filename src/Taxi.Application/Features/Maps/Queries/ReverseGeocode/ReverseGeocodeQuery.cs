using MediatR;
using Taxi.Application.Features.Maps.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Maps.Queries.ReverseGeocode;

public record ReverseGeocodeQuery(
    decimal Latitude,
    decimal Longitude) : IRequest<Result<ReverseGeocodeDto>>;
