using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.Features.Users.Commands.UpdateProfile;
using Taxi.Application.Features.Users.Commands.UploadProfilePhoto;
using Taxi.Application.Features.Users.Queries.GetCurrentUser;
using Taxi.Contracts.Requests.Users;

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

    [HttpPatch("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates the current user's display name.")]
    [EndpointName("UpdateProfile")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var command = new UpdateProfileCommand(request.Name);
        var result = await sender.Send(command, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("me/photo")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Uploads or replaces the current user's profile photo.")]
    [EndpointDescription("Accepts JPEG or PNG, max 5MB. Returns the URL of the stored photo.")]
    [EndpointName("UploadProfilePhoto")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UploadProfilePhoto(IFormFile photo, CancellationToken ct)
    {
        if (photo is null || photo.Length == 0)
        {
            return BadRequest(new ProblemDetails { Title = "No file provided." });
        }

        var command = new UploadProfilePhotoCommand(
            photo.OpenReadStream(),
            photo.FileName,
            photo.ContentType);

        var result = await sender.Send(command, ct);

        return result.Match(
            url => Ok(new { profilePhotoUrl = url }),
            Problem);
    }
}
