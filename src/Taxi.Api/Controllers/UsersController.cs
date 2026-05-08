using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Taxi.Api.Contracts;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.Features.Users.Commands.UpdateUserProfile;
using Taxi.Application.Features.Users.Queries.GetCurrentUser;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public class UsersController(ISender sender) : ApiController
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
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
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
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
}

