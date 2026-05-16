using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Taxi.Application.Features.Payments.Commands.HandleStripeWebhook;

namespace Taxi.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
[AllowAnonymous] // Stripe signature header is the authentication; do not require JWT.
public sealed class WebhooksController(ISender sender, ILogger<WebhooksController> logger) : ControllerBase
{
    [HttpPost("stripe")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Stripe(CancellationToken ct)
    {
        string json;
        using (var reader = new StreamReader(Request.Body))
        {
            json = await reader.ReadToEndAsync(ct);
        }

        var signature = Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrEmpty(signature))
        {
            logger.LogWarning("Stripe webhook received without Stripe-Signature header.");
            return BadRequest();
        }

        var result = await sender.Send(new HandleStripeWebhookCommand(json, signature), ct);

        // 2xx on handled events (including idempotent no-ops and unhandled types) so Stripe stops retrying.
        // 400 only when signature verification failed — Stripe will retry per its delivery policy.
        return result.IsSuccess ? Ok() : BadRequest();
    }
}
