using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Trips.Commands.CancelTrip;
using Taxi.Application.Features.Trips.Commands.GetPricingQuotes;
using Taxi.Application.Features.Trips.Commands.RequestTrip;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Application.Features.Trips.Queries.GetPassengerTripCount;
using Taxi.Application.Features.Trips.Queries.GetPassengerTrips;
using Taxi.Application.Features.Trips.Queries.GetTripById;
using Taxi.Contracts.Requests.Trips;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/trips")]
public class TripsController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Roles = "Passenger,Admin")]
    [ProducesResponseType(typeof(PagedResult<TripSummaryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Returns the current passenger's trip history.")]
    [EndpointName("GetPassengerTrips")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetPassengerTrips([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPassengerTripsQuery(page, pageSize), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("count")]
    [Authorize(Roles = "Passenger,Admin")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [EndpointSummary("Returns the total number of trips for the current passenger.")]
    [EndpointName("GetPassengerTripCount")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetPassengerTripCount(CancellationToken ct)
    {
        var result = await sender.Send(new GetPassengerTripCountQuery(), ct);
        return result.Match(count => Ok(count), Problem);
    }

    [HttpGet("{id:guid}", Name = "GetTripById")]
    [Authorize(Roles = "Passenger,Driver,Admin")]
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

    [HttpPost("quotes")]
    [Authorize(Roles = "Passenger,Admin")]
    [ProducesResponseType(typeof(PricingQuotesListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Returns pricing quotes for all active vehicle types.")]
    [EndpointDescription("Calculates route distance and duration via Google Maps, then returns one quote per vehicle type. Each quote is valid for 15 minutes.")]
    [EndpointName("GetPricingQuotes")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetPricingQuotes([FromBody] GetPricingQuotesRequest request, CancellationToken ct)
    {
        var command = new GetPricingQuotesCommand(
            request.Stops.Select(s => new CoordinateDto(s.Latitude, s.Longitude)).ToList());

        var result = await sender.Send(command, ct);

        return result.Match(
            Ok,
            Problem);
    }

    [HttpPost]
    [Authorize(Roles = "Passenger,Admin")]
    [ProducesResponseType(typeof(TripDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Confirms a trip using a previously obtained pricing quote.")]
    [EndpointDescription("Creates the trip, marks the quote as used, and synchronously assigns the admin driver.")]
    [EndpointName("RequestTrip")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> RequestTrip([FromBody] RequestTripRequest request, CancellationToken ct)
    {
        var command = new RequestTripCommand(
            request.QuoteId,
            request.Stops.Select(s => new CoordinateDto(s.Latitude, s.Longitude)).ToList(),
            request.ScheduledAt);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => CreatedAtRoute(
                routeName: "GetTripById",
                routeValues: new { version = "1.0", id = response.Id },
                value: response),
            Problem);
    }

    [HttpPost("{id:guid}/cancellations")]
    [Authorize(Roles = "Passenger,Admin")]
    [ProducesResponseType(typeof(TripDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Cancels a trip.")]
    [EndpointDescription("Allowed while trip is Scheduled, PendingDriver, or DriverAssigned. Not allowed once InProgress.")]
    [EndpointName("CancelTrip")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> CancelTrip(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new CancelTripCommand(id), ct);
        return result.Match(Ok, Problem);
    }
}

