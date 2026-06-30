using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.RefundIssues.Commands.ReviewRefundIssue;
using Taxi.Application.Features.RefundIssues.Dtos;
using Taxi.Application.Features.RefundIssues.Queries.GetRefundIssueById;
using Taxi.Application.Features.RefundIssues.Queries.GetRefundIssues;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Requests.RefundIssues;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/refund-issues")]
[Authorize(Roles = "Admin")]
public sealed class RefundIssuesController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RefundIssueDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Admin refund issue feed.")]
    [EndpointName("GetRefundIssues")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetRefundIssues(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? reviewStatus = null,
        [FromQuery] string? requestType = null,
        [FromQuery] Guid? tripId = null,
        [FromQuery] Guid? passengerId = null,
        [FromQuery] DateTimeOffset? fromUtc = null,
        [FromQuery] DateTimeOffset? toUtc = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetRefundIssuesQuery(page, pageSize, reviewStatus, requestType, tripId, passengerId, fromUtc, toUtc),
            ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RefundIssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Admin refund issue detail.")]
    [EndpointName("GetRefundIssueById")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetRefundIssueById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetRefundIssueByIdQuery(id), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{id:guid}/review")]
    [ProducesResponseType(typeof(RefundIssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Reviews a refund issue without triggering any Stripe refund.")]
    [EndpointName("ReviewRefundIssue")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> ReviewRefundIssue(Guid id, [FromBody] ReviewRefundIssueRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ReviewRefundIssueCommand(id, request.ReviewStatus, request.AdminNotes), ct);
        return result.Match(Ok, Problem);
    }
}
