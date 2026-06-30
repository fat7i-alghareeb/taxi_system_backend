using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Refunds.Commands.RetryRefund;
using Taxi.Application.Features.Refunds.Dtos;
using Taxi.Application.Features.Refunds.Queries.GetRefundById;
using Taxi.Application.Features.Refunds.Queries.GetRefunds;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Requests.Refunds;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/refunds")]
[Authorize(Roles = "Admin")]
public sealed class RefundsController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminRefundDetailDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Admin refund operations feed with filters.")]
    [EndpointName("GetRefunds")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetRefunds(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? sourceType = null,
        [FromQuery] bool? requiresAdminAction = null,
        [FromQuery] DateTimeOffset? fromUtc = null,
        [FromQuery] DateTimeOffset? toUtc = null,
        [FromQuery] Guid? tripId = null,
        [FromQuery] Guid? passengerId = null,
        [FromQuery] string? paymentMethod = null,
        [FromQuery] decimal? minAmount = null,
        [FromQuery] decimal? maxAmount = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetRefundsQuery(
                page,
                pageSize,
                status,
                sourceType,
                requiresAdminAction,
                fromUtc,
                toUtc,
                tripId,
                passengerId,
                paymentMethod,
                minAmount,
                maxAmount),
            ct);

        return result.Match(Ok, Problem);
    }

    [HttpGet("{refundId:guid}")]
    [ProducesResponseType(typeof(AdminRefundDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Admin refund detail.")]
    [EndpointName("GetRefundById")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetRefundById(Guid refundId, CancellationToken ct)
    {
        var result = await sender.Send(new GetRefundByIdQuery(refundId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{refundId:guid}/retry")]
    [ProducesResponseType(typeof(AdminRefundDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retries a failed refund after backend eligibility is rechecked.")]
    [EndpointName("RetryRefund")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> RetryRefund(Guid refundId, [FromBody] RetryRefundRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RetryRefundCommand(refundId, request.Note), ct);
        return result.Match(Ok, Problem);
    }
}
