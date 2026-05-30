using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Trips.Commands.AdminTakeTrip;
using Taxi.Application.Features.Trips.Commands.ArriveTrip;
using Taxi.Application.Features.Trips.Commands.AssignDriverToTrip;
using Taxi.Application.Features.Trips.Commands.CancelTrip;
using Taxi.Application.Features.Trips.Commands.CompleteStop;
using Taxi.Application.Features.Trips.Commands.CompleteTrip;
using Taxi.Application.Features.Trips.Commands.DriverCancelTrip;
using Taxi.Application.Features.Trips.Commands.EnRouteTrip;
using Taxi.Application.Features.Trips.Commands.GetPricingQuotes;
using Taxi.Application.Features.Trips.Commands.RequestTrip;
using Taxi.Application.Features.Trips.Commands.ResendArrivedNotification;
using Taxi.Application.Features.Trips.Commands.ReviewCompensationClaim;
using Taxi.Application.Features.Trips.Commands.StartTrip;
using Taxi.Application.Features.Trips.Commands.StartTripWaiting;
using Taxi.Application.Features.Trips.Commands.StopTripWaiting;
using Taxi.Application.Features.Trips.Commands.SubmitCompensationClaim;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Application.Features.Trips.Queries.GetAllTrips;
using Taxi.Application.Features.Trips.Queries.GetCompensationClaims;
using Taxi.Application.Features.Trips.Queries.GetPassengerTripCount;
using Taxi.Application.Features.Trips.Queries.GetPassengerTrips;
using Taxi.Application.Features.Trips.Queries.GetTripById;
using Taxi.Application.Features.Trips.Queries.GetTripDetails;
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
            request.Stops.Select(s => new CoordinateDto(s.Latitude, s.Longitude, s.Label)).ToList(),
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
    public async Task<IActionResult> CancelTrip(Guid id, [FromBody] CancelTripRequest? request, CancellationToken ct)
    {
        var result = await sender.Send(new CancelTripCommand(id, request?.Reason, request?.Note), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/driver-cancellations")]
    [Authorize(Roles = "Driver,Admin")]
    [ProducesResponseType(typeof(TripDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Driver cancels a trip because the passenger is late, no-show, or unreachable.")]
    [EndpointName("DriverCancelTrip")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> DriverCancelTrip(Guid id, [FromBody] DriverCancelTripRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new DriverCancelTripCommand(id, request.Reason, request.Note), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/compensation-claims")]
    [Authorize(Roles = "Passenger,Admin")]
    [ProducesResponseType(typeof(CompensationClaimDto), StatusCodes.Status200OK)]
    [EndpointSummary("Submits a driver-late compensation claim for admin review.")]
    [EndpointName("SubmitCompensationClaim")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> SubmitCompensationClaim(Guid id, [FromBody] SubmitCompensationClaimRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SubmitCompensationClaimCommand(id, request.Note, request.EvidenceUrls), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("compensation-claims")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<CompensationClaimDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Returns compensation claims for admin review.")]
    [EndpointName("GetCompensationClaims")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetCompensationClaims([FromQuery] string? status = null, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetCompensationClaimsQuery(status), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("compensation-claims/{claimId:guid}/review")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CompensationClaimDto), StatusCodes.Status200OK)]
    [EndpointSummary("Approves or rejects a compensation claim.")]
    [EndpointName("ReviewCompensationClaim")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> ReviewCompensationClaim(Guid claimId, [FromBody] ReviewCompensationClaimRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ReviewCompensationClaimCommand(claimId, request.Approved, request.Notes), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/waiting/start")]
    [Authorize(Roles = "Driver,Admin")]
    [ProducesResponseType(typeof(WaitingSessionDto), StatusCodes.Status200OK)]
    [EndpointSummary("Starts driver waiting-time tracking.")]
    [EndpointName("StartTripWaiting")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> StartWaiting(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new StartTripWaitingCommand(id), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/waiting/stop")]
    [Authorize(Roles = "Driver,Admin")]
    [ProducesResponseType(typeof(WaitingSessionDto), StatusCodes.Status200OK)]
    [EndpointSummary("Stops driver waiting-time tracking and returns estimated fee.")]
    [EndpointName("StopTripWaiting")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> StopWaiting(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new StopTripWaitingCommand(id), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/en-route")]
    [Authorize(Roles = "Driver,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Driver starts driving to pickup location.")]
    [EndpointName("EnRouteTrip")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> EnRoute(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new EnRouteTripCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/arrive")]
    [Authorize(Roles = "Driver,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Driver arrives at pickup location.")]
    [EndpointName("ArriveTrip")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Arrive(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ArriveTripCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/arrive/resend")]
    [Authorize(Roles = "Driver,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Re-broadcasts the driver-arrived notification (FCM + SignalR) without changing trip state.")]
    [EndpointName("ResendArrivedNotification")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> ResendArriveNotification(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ResendArrivedNotificationCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(Roles = "Driver,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Driver starts the trip (passenger is in the vehicle).")]
    [EndpointName("StartTrip")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> StartTrip(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new StartTripCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Driver,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Driver completes the trip at the destination.")]
    [EndpointName("CompleteTrip")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new CompleteTripCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/stops/{sequence:int}/complete")]
    [Authorize(Roles = "Driver,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Driver marks an intermediate stop as completed on a multi-stop trip.")]
    [EndpointDescription("Stops must be completed in ascending sequence order. The final dropoff is reached via /complete, not this endpoint.")]
    [EndpointName("CompleteTripStop")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> CompleteStop(Guid id, int sequence, CancellationToken ct)
    {
        var result = await sender.Send(new CompleteStopCommand(id, sequence), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Admin manually dispatches a driver to a trip.")]
    [EndpointName("AssignDriverToTrip")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] Guid driverId, CancellationToken ct)
    {
        var result = await sender.Send(new AssignDriverToTripCommand(id, driverId), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/admin-take")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Admin takes a trip and acts as its driver.")]
    [EndpointDescription("Ensures the admin has a backing Driver record (creating one on first use), then assigns the trip to it.")]
    [EndpointName("AdminTakeTrip")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> AdminTake(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new AdminTakeTripCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<TripDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Admin retrieves a paginated and filtered list of all trips.")]
    [EndpointName("GetAllTripsAdmin")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetAllTripsAdmin([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null, [FromQuery] Guid? driverId = null, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAllTripsQuery(page, pageSize, status, driverId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}/details")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TripDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Admin retrieves full detailed information about a trip.")]
    [EndpointName("GetTripDetailsAdmin")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetDetails(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetTripDetailsQuery(id), ct);
        return result.Match(Ok, Problem);
    }
}

