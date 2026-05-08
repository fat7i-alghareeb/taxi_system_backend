using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Maps.Dtos;
using Taxi.Application.Features.Maps.Queries.GetDirections;
using Taxi.Application.Features.Maps.Queries.ReverseGeocode;
using Taxi.Application.Features.Maps.Queries.SearchPlaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Requests.Maps;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/maps")]
public class MapsController(ISender sender) : ApiController
{
    [HttpPost("search")]
    [ProducesResponseType(typeof(List<PlaceResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Search for places by text query.")]
    [EndpointDescription("Calls Google Places Text Search API. Optionally biases results around the provided coordinates.")]
    [EndpointName("SearchPlaces")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> SearchPlaces([FromBody] SearchPlacesRequest request, CancellationToken ct)
    {
        var query = new SearchPlacesQuery(request.Query, request.Latitude, request.Longitude);
        var result = await sender.Send(query, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("reverse-geocodings")]
    [ProducesResponseType(typeof(ReverseGeocodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Converts coordinates to a human-readable address.")]
    [EndpointName("ReverseGeocode")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> ReverseGeocode([FromBody] ReverseGeocodeRequest request, CancellationToken ct)
    {
        var query = new ReverseGeocodeQuery(request.Latitude, request.Longitude);
        var result = await sender.Send(query, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("directions")]
    [ProducesResponseType(typeof(DirectionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Calculates route between two or more stops.")]
    [EndpointDescription("Returns total distance, duration, encoded polyline, and per-leg breakdown.")]
    [EndpointName("GetDirections")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetDirections([FromBody] GetDirectionsRequest request, CancellationToken ct)
    {
        var stops = request.Stops.Select(s => new CoordinateDto(s.Latitude, s.Longitude)).ToList();
        var query = new GetDirectionsQuery(stops);
        var result = await sender.Send(query, ct);
        return result.Match(Ok, Problem);
    }
}

