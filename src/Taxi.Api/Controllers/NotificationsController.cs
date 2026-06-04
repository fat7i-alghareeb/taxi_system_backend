using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Notifications.Commands.BroadcastNotification;
using Taxi.Contracts.Notifications;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Authorize(Roles = "Admin")]
public class NotificationsController(ISender sender) : ApiController
{
    [HttpPost("broadcast")]   // Deprecated alias kept for the Blazor client; Flutter calls POST /broadcasts.
    [HttpPost("broadcasts")]  // Constitution-compliant noun.
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Admin broadcasts a push notification to a specific target audience.")]
    [EndpointDescription("Translates the audience enum to the secure FCM topic name internally.")]
    [EndpointName("BroadcastNotification")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastNotificationRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new BroadcastNotificationCommand(
                request.Audience,
                request.Title,
                request.Body,
                request.Data),
            ct);

        return result.Match(_ => NoContent(), Problem);
    }
}

public record BroadcastNotificationRequest(
    NotificationAudience Audience,
    string Title,
    string Body,
    Dictionary<string, string>? Data = null);
