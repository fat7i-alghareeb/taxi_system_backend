using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Config.GetTripDiscount;
using Taxi.Application.Features.Config.UpdateTripDiscount;
using Taxi.Contracts.Requests.Config;
using Taxi.Contracts.Responses.Config;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/app-config")]
public class AppConfigController(ISender sender) : ApiController
{
    [HttpGet("trip-discount")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TripDiscountDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets the global trip discount percentage.")]
    [EndpointName("GetTripDiscount")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetTripDiscount(CancellationToken ct)
    {
        var result = await sender.Send(new GetTripDiscountQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("trip-discount")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TripDiscountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates the global trip discount percentage (0–100).")]
    [EndpointName("UpdateTripDiscount")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdateTripDiscount(
        [FromBody] UpdateTripDiscountRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdateTripDiscountCommand(request.DiscountPercent), ct);
        return result.Match(Ok, Problem);
    }
}
