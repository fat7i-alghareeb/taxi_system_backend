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

        var legs = new List<LegDto>();
        var totalDistanceMeters = 0;
        var totalDurationSeconds = 0;

        for (var i = 0; i < request.Stops.Count - 1; i++)
        {
            var origin = request.Stops[i];
            var destination = request.Stops[i + 1];

            var leg = await directionsService.GetDirectionsAsync(
                origin.Latitude, origin.Longitude,
                destination.Latitude, destination.Longitude);

            totalDistanceMeters += leg.DistanceMeters;
            totalDurationSeconds += leg.DurationSeconds;
            legs.Add(new LegDto(leg.DistanceMeters, leg.DurationSeconds, leg.EncodedPolyline));
        }

        // Use the polyline of the first leg as the overall polyline when there is only one,
        // otherwise concatenate them (client merges for display).
        var overallPolyline = legs.Count == 1
            ? legs[0].EncodedPolyline
            : string.Join("|", legs.Select(l => l.EncodedPolyline));

        return new DirectionsDto(totalDistanceMeters, totalDurationSeconds, overallPolyline, legs);
    }
}
