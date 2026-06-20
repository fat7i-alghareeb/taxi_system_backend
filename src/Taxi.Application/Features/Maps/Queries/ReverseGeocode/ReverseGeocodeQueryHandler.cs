using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Maps.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Maps.Queries.ReverseGeocode;

public class ReverseGeocodeQueryHandler(IGeocodingService geocodingService)
    : IRequestHandler<ReverseGeocodeQuery, Result<ReverseGeocodeDto>>
{
    public async Task<Result<ReverseGeocodeDto>> Handle(ReverseGeocodeQuery request, CancellationToken ct)
    {
        var result = await geocodingService.ReverseGeocodeAsync(request.Latitude, request.Longitude);

        if (result is null)
        {
            return Error.NotFound(LocalizationKeys.Maps.CoordinateInvalid, "No address found for the given coordinates.");
        }

        return new ReverseGeocodeDto(
            result.PrimaryName,
            result.SecondaryAddress,
            result.Latitude,
            result.Longitude,
            result.IsAirport);
    }
}

