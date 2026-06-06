using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Taxi.Application.Features.Payments.Commands.HandleStripeWebhook;

namespace Taxi.Api.Controllers;

// Webhooks are deliberately unversioned ("/api/webhooks/...") for stable third-party
// callback URLs. Stripe authenticates via the Stripe-Signature header (validated by
// IStripeWebhookValidator), so this controller is AllowAnonymous.
[ApiController]
[Route("api/webhooks")]
[AllowAnonymous]
public sealed class WebhooksController(ISender sender) : ControllerBase
{
    [HttpPost("stripe")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Receives Stripe webhook events (payment_intent.succeeded, payment_intent.payment_failed, payment_intent.canceled, charge.refunded).")]
    [EndpointDescription("The raw HTTP body is forwarded to HandleStripeWebhookCommand which verifies the signature and dispatches to domain handlers idempotently. Reads body before model binding because Stripe signature verification requires the byte-for-byte payload.")]
    [EndpointName("StripeWebhook")]
    public async Task<IActionResult> Stripe(CancellationToken ct)
    {
        // Read the raw body — required for Stripe signature verification.
        // Cannot live in a handler because it is an HTTP-binding concern.
        string json;
        using (var reader = new StreamReader(Request.Body))
        {
            json = await reader.ReadToEndAsync(ct);
        }

        var signature = Request.Headers["Stripe-Signature"].ToString();

        var result = await sender.Send(new HandleStripeWebhookCommand(json, signature), ct);

        // Stripe contract: 2xx => stop retrying; non-2xx => retry per delivery policy.
        // Failures (signature invalid, intent not found, handler errors) all map to 400.
        return result.IsSuccess ? Ok() : BadRequest();
    }
}
