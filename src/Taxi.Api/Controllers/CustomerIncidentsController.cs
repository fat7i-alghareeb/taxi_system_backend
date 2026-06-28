using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Taxi.Application.Features.CustomerIncidents.Commands.ChangeIncidentStatus;
using Taxi.Application.Features.CustomerIncidents.Commands.ContactPassenger;
using Taxi.Application.Features.CustomerIncidents.Commands.RefundIncident;
using Taxi.Application.Features.CustomerIncidents.Dtos;
using Taxi.Application.Features.CustomerIncidents.Queries.GetCustomerIncidentDetail;
using Taxi.Application.Features.CustomerIncidents.Queries.GetCustomerIncidents;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customer-incidents")]
[Authorize(Roles = "Admin")]
public sealed class CustomerIncidentsController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<CustomerIncidentDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Admin incident feed. All filters optional; none = everything.")]
    [EndpointName("GetCustomerIncidents")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetCustomerIncidents(
        [FromQuery] string? type = null,
        [FromQuery] Guid? passengerId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? severity = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetCustomerIncidentsQuery(type, passengerId, status, severity, page, pageSize),
            ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerIncidentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Full incident detail (recordings, chat, locations). Access is audit-logged.")]
    [EndpointName("GetCustomerIncidentDetail")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetCustomerIncidentDetail(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetCustomerIncidentDetailQuery(id), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(typeof(CustomerIncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Triage: set incident status (Open/InReview/Resolved/Dismissed) with an optional note.")]
    [EndpointName("ChangeIncidentStatus")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeIncidentStatusRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ChangeIncidentStatusCommand(id, request.Status, request.Note), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Contact the customer: send a push message to the incident's passenger.")]
    [EndpointName("ContactIncidentPassenger")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> ContactPassenger(Guid id, [FromBody] ContactPassengerRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ContactPassengerCommand(id, request.Title, request.Body), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/refunds")]
    [ProducesResponseType(typeof(CustomerIncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Issue a Stripe refund against the incident's trip fare (null amount = full refund).")]
    [EndpointName("RefundIncident")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundIncidentRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RefundIncidentCommand(id, request.Amount), ct);
        return result.Match(Ok, Problem);
    }
}

public record ChangeIncidentStatusRequest(string Status, string? Note);
public record ContactPassengerRequest(string Title, string Body);
public record RefundIncidentRequest(decimal? Amount);
