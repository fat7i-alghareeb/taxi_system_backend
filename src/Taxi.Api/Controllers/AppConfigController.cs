using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Config.GetClientConfig;
using Taxi.Application.Features.Config.GetCurrency;
using Taxi.Application.Features.Config.GetTripDiscount;
using Taxi.Application.Features.Config.UpdateCurrency;
using Taxi.Application.Features.Config.UpdateTripDiscount;
using Taxi.Contracts.Requests.Config;
using Taxi.Contracts.Responses.Config;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/app-config")]
public class AppConfigController(ISender sender) : ApiController
{
    [HttpGet("client")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ClientConfigDto), StatusCodes.Status200OK)]
    [EndpointSummary("Returns public client configuration (Stripe enablement, publishable key).")]
    [EndpointDescription("Consumed by the mobile app at bootstrap to decide whether to initialize Stripe and which publishable key to use.")]
    [EndpointName("GetClientConfig")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetClientConfig(CancellationToken ct)
    {
        var result = await sender.Send(new GetClientConfigQuery(), ct);
        return result.Match(Ok, Problem);
    }

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

    [HttpGet("currency")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets the global currency (ISO 4217 code) used for Stripe payments.")]
    [EndpointName("GetCurrency")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetCurrency(CancellationToken ct)
    {
        var result = await sender.Send(new GetCurrencyQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("currency")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CurrencyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates the global currency (ISO 4217 3-letter code) used for Stripe payments.")]
    [EndpointName("UpdateCurrency")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdateCurrency(
        [FromBody] UpdateCurrencyRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdateCurrencyCommand(request.CurrencyCode), ct);
        return result.Match(Ok, Problem);
    }
}
