using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
using Taxi.Application.Features.Trips.Commands.RateTrip;
using Taxi.Application.Features.Trips.Commands.RequestTrip;
using Taxi.Application.Features.Trips.Commands.ResendArrivedNotification;
using Taxi.Application.Features.Trips.Commands.ReviewCompensationClaim;
using Taxi.Application.Features.Trips.Commands.SettleWaitingFee;
using Taxi.Application.Features.Trips.Commands.StartTrip;
using Taxi.Application.Features.Trips.Commands.StartTripWaiting;
using Taxi.Application.Features.Trips.Commands.StopTripWaiting;
using Taxi.Application.Features.Trips.Commands.SubmitCompensationClaim;
using Taxi.Application.Features.Trips.Commands.UpdatePassengerNote;
using Taxi.Application.Features.Trips.Commands.UploadTripRecording;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Application.Features.Trips.Queries.GetAllTrips;
using Taxi.Application.Features.Trips.Queries.GetCompensationClaims;
using Taxi.Application.Features.Trips.Queries.GetMyActiveTrip;
using Taxi.Application.Features.Trips.Queries.GetPassengerTripCount;
using Taxi.Application.Features.Trips.Queries.GetPassengerTrips;
using Taxi.Application.Features.Trips.Queries.GetTripById;
using Taxi.Application.Features.Trips.Queries.GetTripDetails;
using Taxi.Application.Features.Trips.Queries.GetTripInvoice;
using Taxi.Application.Features.Trips.Queries.GetTripInvoicePdf;
using Taxi.Application.Features.Trips.Queries.GetTripReceipt;
using Taxi.Application.Features.Uploads.Commands.UploadCompensationEvidence;
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
    public async Task<IActionResult> GetPassengerTrips([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPassengerTripsQuery(page, pageSize, search), ct);
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

    [HttpGet("active")]
    [Authorize(Roles = "Passenger,Driver,Admin")]
    [ProducesResponseType(typeof(TripDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Returns the caller's current active (non-terminal) trip, or 204 if none.")]
    [EndpointDescription("Used by the apps to resume the live trip view and join its realtime channel on launch / tab open. Resolves the passenger's trip, then the driver's assignment.")]
    [EndpointName("GetMyActiveTrip")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetMyActiveTrip(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyActiveTripQuery(), ct);
        return result.Match(dto => dto is null ? NoContent() : Ok(dto), Problem);
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
            request.Stops.Select(s => new CoordinateDto(
                s.Latitude,
                s.Longitude,
                s.Label,
                s.IsAirport)).ToList(),
            request.ScheduledAt,
            request.PassengerNote,
            request.FlightNumber);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => CreatedAtRoute(
                routeName: "GetTripById",
                routeValues: new { version = "1.0", id = response.Id },
                value: response),
            Problem);
    }

    [HttpPut("{id:guid}/passenger-note")]
    [Authorize(Roles = "Passenger,Admin")]
    [ProducesResponseType(typeof(TripDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Updates the passenger note shown to the driver before the trip starts.")]
    [EndpointName("UpdatePassengerNote")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdatePassengerNote(Guid id, [FromBody] UpdatePassengerNoteRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdatePassengerNoteCommand(id, request.PassengerNote), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/cancellations")]
    [Authorize(Roles = "Passenger,Admin")]
    [ProducesResponseType(typeof(TripDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Cancels a trip.")]
    [EndpointDescription("Cancellable until the ride is InProgress. Free (100% refund) within 1 hour of booking; after that 20% is refunded. Not allowed once InProgress, Completed, Cancelled, Refunded, or PaymentFailed.")]
    [EndpointName("CancelTrip")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> CancelTrip(Guid id, [FromBody] CancelTripRequest? request, CancellationToken ct)
    {
        var result = await sender.Send(new CancelTripCommand(id, request?.Reason, request?.Note), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/driver-cancellations")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TripDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Owning admin cancels an arrived trip because the passenger is late, no-show, or unreachable.")]
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

    [HttpPost("{id:guid}/recordings")]
    [Authorize(Roles = "Passenger")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Uploads an in-trip safety audio recording and returns its URL.")]
    [EndpointName("UploadTripRecording")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UploadTripRecording(
        Guid id,
        [FromForm] IFormFile file,
        [FromForm] int? durationSeconds,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("No file was uploaded.");
        }

        // Map IFormFile → UploadFileItem so the Application layer never sees ASP.NET types.
        var item = new UploadFileItem(file.OpenReadStream(), file.FileName);

        var result = await sender.Send(new UploadTripRecordingCommand(id, item, durationSeconds), ct);

        return result.Match(
            url => Ok(new { url }),
            Problem);
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

    [HttpPost("compensation-claims/{claimId:guid}/review")] // Deprecated alias.
    [HttpPost("compensation-claims/{claimId:guid}/reviews")] // Constitution-compliant noun.
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CompensationClaimDto), StatusCodes.Status200OK)]
    [EndpointSummary("Approves or rejects a compensation claim.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> ReviewCompensationClaim(Guid claimId, [FromBody] ReviewCompensationClaimRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ReviewCompensationClaimCommand(claimId, request.Approved, request.Notes), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/waiting/start")] // Deprecated alias (verb + 2-level depth).
    [HttpPost("{id:guid}/waiting-sessions")] // Constitution-compliant noun (creates a session resource).
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(WaitingSessionDto), StatusCodes.Status200OK)]
    [EndpointSummary("Starts driver waiting-time tracking.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> StartWaiting(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new StartTripWaitingCommand(id), ct);
        return result.Match(Ok, Problem);
    }

    // Constitution-compliant: DELETE on the singleton current waiting session ends it.
    [HttpDelete("{id:guid}/waiting-sessions/current")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(WaitingSessionDto), StatusCodes.Status200OK)]
    [EndpointSummary("Stops driver waiting-time tracking and returns estimated fee.")]
    [EndpointName("StopTripWaiting")]
    [MapToApiVersion("1.0")]
    public Task<IActionResult> StopWaiting(Guid id, CancellationToken ct)
        => StopWaitingCore(id, ct);

    // Deprecated legacy alias kept for the Blazor client (verb + 2-level depth).
    [HttpPost("{id:guid}/waiting/stop")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(WaitingSessionDto), StatusCodes.Status200OK)]
    [EndpointSummary("[Deprecated] Use DELETE /waiting-sessions/current. Stops driver waiting-time tracking.")]
    [EndpointName("StopTripWaitingLegacy")]
    [MapToApiVersion("1.0")]
    public Task<IActionResult> StopWaitingLegacy(Guid id, CancellationToken ct)
        => StopWaitingCore(id, ct);

    [HttpPost("{id:guid}/en-route")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Owning admin marks the ride as on the way to pickup.")]
    [EndpointName("EnRouteTrip")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> EnRoute(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new EnRouteTripCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/arrive")] // Deprecated alias.
    [HttpPost("{id:guid}/arrivals")] // Constitution-compliant noun.
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Owning admin marks the ride as arrived at pickup.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Arrive(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ArriveTripCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/arrive/resend")] // Deprecated alias (verb + 2-level).
    [HttpPost("{id:guid}/arrival-notifications")] // Constitution-compliant noun (POST creates a new notification dispatch).
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Re-broadcasts the driver-arrived notification (FCM + SignalR) without changing trip state.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> ResendArriveNotification(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ResendArrivedNotificationCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/start")] // Deprecated alias.
    [HttpPost("{id:guid}/starts")] // Constitution-compliant noun.
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Owning admin starts the trip after pickup.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> StartTrip(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new StartTripCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/complete")] // Deprecated alias.
    [HttpPost("{id:guid}/completions")] // Constitution-compliant noun.
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Owning admin completes the trip at the destination.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new CompleteTripCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/rating")] // Simple alias used by the mobile apps.
    [HttpPost("{id:guid}/ratings")] // Constitution-compliant noun (creates a rating resource).
    [Authorize(Roles = "Passenger,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Passenger rates a completed trip (1-5 stars).")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> RateTrip(Guid id, [FromBody] RateTripRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RateTripCommand(id, request.Stars, request.Comment), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/waiting-fee/settlements")]
    [Authorize(Roles = "Passenger,Admin")]
    [ProducesResponseType(typeof(WaitingFeeSettlementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Creates a payment to settle an outstanding waiting fee for a trip.")]
    [EndpointName("SettleWaitingFee")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> SettleWaitingFee(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new SettleWaitingFeeCommand(id), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/stops/{sequence:int}/complete")] // Deprecated alias.
    [HttpPost("{id:guid}/stops/{sequence:int}/completions")] // Constitution-compliant noun.
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Owning admin marks an intermediate stop as completed on a multi-stop trip.")]
    [EndpointDescription("Stops must be completed in ascending sequence order. The final dropoff is reached via /completions, not this endpoint.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> CompleteStop(Guid id, int sequence, CancellationToken ct)
    {
        var result = await sender.Send(new CompleteStopCommand(id, sequence), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/assign")] // Deprecated alias.
    [HttpPost("{id:guid}/assignments")] // Constitution-compliant noun.
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("[Compatibility] Accepts the trip for the current admin.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] Guid driverId, CancellationToken ct)
    {
        var result = await sender.Send(new AssignDriverToTripCommand(id, driverId), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/admin-take")] // Deprecated alias.
    [HttpPost("{id:guid}/admin-takeovers")] // Constitution-compliant noun.
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Admin accepts ownership of a paid trip.")]
    [EndpointDescription("The first admin to accept wins. No backing Driver record is created.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> AdminTake(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new AdminTakeTripCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResult<TripDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Admin retrieves a paginated and filtered list of all trips.")]
    [EndpointName("GetAllTripsAdmin")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetAllTripsAdmin([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null, [FromQuery] Guid? driverId = null, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAllTripsQuery(page, pageSize, status, driverId, search), ct);
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

    [HttpGet("{id:guid}/receipt")]
    [Authorize(Roles = "Passenger,Admin")]
    [ProducesResponseType(typeof(TripReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Returns a customer-facing receipt for a completed trip.")]
    [EndpointName("GetTripReceipt")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetReceipt(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetTripReceiptQuery(id), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}/invoice")]
    [Authorize(Roles = "Passenger,Admin")]
    [ProducesResponseType(typeof(TripInvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Returns the issued invoice for a completed trip.")]
    [EndpointName("GetTripInvoice")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetTripInvoiceQuery(id), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}/invoice/pdf")]
    [Authorize(Roles = "Passenger,Admin")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Returns the Fat7i-branded invoice PDF for a completed trip.")]
    [EndpointDescription("Pass ?language=nl|en|ar|de|es|fr|pl|ro|uk to choose the invoice language. Defaults to Dutch (nl); unknown values fall back to Dutch.")]
    [EndpointName("GetTripInvoicePdf")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetInvoicePdf(Guid id, [FromQuery] string? language = null, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetTripInvoicePdfQuery(id, language), ct);
        return result.Match(pdf => File(pdf.Bytes, "application/pdf", pdf.FileName), Problem);
    }

    private async Task<IActionResult> StopWaitingCore(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new StopTripWaitingCommand(id), ct);
        return result.Match(Ok, Problem);
    }
}

