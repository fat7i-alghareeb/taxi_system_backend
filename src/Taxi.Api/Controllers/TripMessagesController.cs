using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Taxi.Application.Features.Trips.Commands.SendTripMessage;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Application.Features.Trips.Queries.GetTripMessages;
using Taxi.Application.Features.Uploads.Commands.UploadCompensationEvidence;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/trips/{tripId:guid}/messages")]
[Authorize(Roles = "Passenger,Driver,Admin")]
public class TripMessagesController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TripMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Returns the in-trip chat history (oldest first).")]
    [EndpointName("GetTripMessages")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetMessages(Guid tripId, CancellationToken ct)
    {
        var result = await sender.Send(new GetTripMessagesQuery(tripId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TripMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Sends a chat message (text and/or a single photo) on a live trip.")]
    [EndpointName("SendTripMessage")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> SendMessage(
        Guid tripId,
        [FromForm] string? content,
        IFormFile? photo,
        CancellationToken ct)
    {
        // Map IFormFile → UploadFileItem so the Application layer never sees ASP.NET types.
        UploadFileItem? photoItem = photo is { Length: > 0 }
            ? new UploadFileItem(photo.OpenReadStream(), photo.FileName)
            : null;

        var result = await sender.Send(new SendTripMessageCommand(tripId, content, photoItem), ct);
        return result.Match(Ok, Problem);
    }
}
