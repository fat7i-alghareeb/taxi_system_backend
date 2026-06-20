using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Maps.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Maps.Queries.SearchPlaces;

public class SearchPlacesQueryHandler(IGeocodingService geocodingService)
    : IRequestHandler<SearchPlacesQuery, Result<List<PlaceResultDto>>>
{
    public async Task<Result<List<PlaceResultDto>>> Handle(SearchPlacesQuery request, CancellationToken ct)
    {
        var results = await geocodingService.SearchPlacesAsync(request.Query, request.Latitude, request.Longitude);

        return results.Select(r => new PlaceResultDto(
            r.PlaceId,
            r.PrimaryName,
            r.SecondaryAddress,
            r.Latitude,
            r.Longitude,
            r.IsAirport)).ToList();
    }
}

