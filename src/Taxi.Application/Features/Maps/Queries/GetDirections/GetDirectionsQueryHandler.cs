using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Maps.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Maps.Queries.GetDirections;

public class GetDirectionsQueryHandler(IDirectionsService directionsService)
    : IRequestHandler<GetDirectionsQuery, Result<DirectionsDto>>
{
    public async Task<Result<DirectionsDto>> Handle(GetDirectionsQuery request, CancellationToken ct)
    {
        if (request.Stops.Count < 2)
        {
            return Error.Validation(LocalizationKeys.Maps.InsufficientStops, "At least two stops are required.");
        }

        var stops = request.Stops.Select(s => new Coordinate(s.Latitude, s.Longitude)).ToList();
        var directionResponse = await directionsService.GetDirectionsAsync(stops);

        var legs = directionResponse.Legs.Select(l => new LegDto(
            l.DistanceMeters,
            l.DurationSeconds,
            l.EncodedPolyline,
            l.StartLabel,
            l.EndLabel,
            l.StartCoordinate.Latitude,
            l.StartCoordinate.Longitude,
            l.EndCoordinate.Latitude,
            l.EndCoordinate.Longitude,
            l.StartAddress,
            l.EndAddress)).ToList();

        return new DirectionsDto(
            directionResponse.TotalDistanceMeters,
            directionResponse.TotalDurationSeconds,
            directionResponse.OverviewPolyline,
            legs);
    }
}

