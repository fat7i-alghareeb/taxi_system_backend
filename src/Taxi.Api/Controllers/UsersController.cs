using System.Security.Claims;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Api.Contracts;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.Features.Identity.Dtos;
using Taxi.Application.Features.Identity.Queries.GetUserInfo;
using Taxi.Application.Features.Users.Commands.DeleteCurrentUser;
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

    // Constitution-compliant: claims are a sub-resource of the authenticated user.
    // Moved from IdentityController (GET /identity/current-user/claims);
    // the legacy route remains in IdentityController for the Blazor client.
    [HttpGet("me/claims")]
    [Authorize]
    [ProducesResponseType(typeof(AppUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Gets the claims of the currently authenticated user.")]
    [EndpointDescription("Returns the claims encoded in the access token, resolved through the Identity service.")]
    [EndpointName("GetCurrentUserClaims")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetCurrentUserClaims(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await sender.Send(new GetUserByIdQuery(userId), ct);
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
            request.Email,
            request.Photo?.OpenReadStream(),
            request.Photo?.ContentType,
            request.HomeAddressLabel,
            request.HomeAddressLatitude,
            request.HomeAddressLongitude);

        var result = await sender.Send(command, ct);
        return result.Match(Ok, Problem);
    }

    [HttpDelete("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Soft-deletes the current authenticated user's account.")]
    [EndpointDescription("Marks the user as deleted (DeletedAtUtc) so historical trips and invoices remain intact while the user can no longer authenticate. This satisfies the in-app account deletion requirement for app store review.")]
    [EndpointName("DeleteCurrentUser")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> DeleteCurrentUser(CancellationToken ct)
    {
        var result = await sender.Send(new DeleteCurrentUserCommand(), ct);
        return result.Match(_ => NoContent(), Problem);
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

