using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Api.Contracts;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.Features.Users.Commands.UpdateFcmToken;
using Taxi.Application.Features.Users.Commands.UpdatePreferredLanguage;
using Taxi.Application.Features.Users.Commands.UpdateUserProfile;
using Taxi.Application.Features.Users.Queries.GetAllUsers;
using Taxi.Application.Features.Users.Queries.GetCurrentUser;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public class UsersController(ISender sender) : ApiController
{
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(Taxi.Application.Features.Auth.Dtos.UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Returns the current authenticated user's profile.")]
    [EndpointName("GetCurrentUser")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var result = await sender.Send(new GetCurrentUserQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("me")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(Taxi.Application.Features.Auth.Dtos.UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates the current user's profile info and photo.")]
    [EndpointDescription("Accepts multipart/form-data. Both name and photo are optional.")]
    [EndpointName("UpdateUserProfile")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateUserProfileRequest request, CancellationToken ct)
    {
        var command = new UpdateUserProfileCommand(
            request.Name,
            request.Photo?.OpenReadStream(),
            request.Photo?.ContentType);

        var result = await sender.Send(command, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("me/fcm-token")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates the current user's FCM device token.")]
    [EndpointName("UpdateFcmToken")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdateFcmToken([FromBody] UpdateFcmTokenRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateFcmTokenCommand(request.FcmToken), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPut("me/language")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates the current user's preferred language code.")]
    [EndpointName("UpdatePreferredLanguage")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdatePreferredLanguage([FromBody] UpdatePreferredLanguageRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdatePreferredLanguageCommand(request.LanguageCode), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<Taxi.Application.Features.Users.Queries.GetAllUsers.UserDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Admin retrieves a paginated list of all registered users.")]
    [EndpointName("GetAllUsersAdmin")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 15, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAllUsersQuery(page, pageSize), ct);
        return result.Match(Ok, Problem);
    }
}

public record UpdateFcmTokenRequest(string FcmToken);
public record UpdatePreferredLanguageRequest(string LanguageCode);

