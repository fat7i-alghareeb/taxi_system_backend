using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Trips.Commands.RequestTrip;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Application.Features.Trips.Queries.GetTripById;
using Taxi.Contracts.Requests.Trips;
using Taxi.Domain.Common.Results;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/trips")]
public class TripsController(ISender sender) : ApiController
{
    [HttpGet("{id:guid}", Name = "GetTripById")]
    [ProducesResponseType(typeof(TripDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves a trip by ID.")]
    [EndpointName("GetTripById")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetTripByIdQuery(id), ct);

        return result.Match(
            Ok,
            Problem);
    }

    [HttpPost("request")]
    [ProducesResponseType(typeof(TripDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Requests a new trip.")]
    [EndpointName("RequestTrip")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> RequestTrip([FromBody] RequestTripRequest request, CancellationToken ct)
    {
        var command = new RequestTripCommand(
            request.VehicleTypeId,
            request.QuoteId,
            request.Stops.Select(s => new TripStopDto(s.Latitude, s.Longitude, s.Label)).ToList());

        var result = await sender.Send(command, ct);

        return result.Match(
            response => CreatedAtRoute(
                routeName: "GetTripById",
                routeValues: new { version = "1.0", id = response.Id },
                value: response),
            Problem);
    }
}
