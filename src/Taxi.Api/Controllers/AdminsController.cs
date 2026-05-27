using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Admins.Commands.UpdateCurrentAdminProfile;
using Taxi.Application.Features.Admins.Dtos;
using Taxi.Application.Features.Admins.Queries.GetCurrentAdminProfile;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
[Route("api/v{version:apiVersion}/admins")]
public sealed class AdminsController(ISender sender) : ApiController
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(AdminProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Returns the current admin profile.")]
    [EndpointName("GetCurrentAdminProfile")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetCurrentAdminProfile(CancellationToken ct)
    {
        var result = await sender.Send(new GetCurrentAdminProfileQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(AdminProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Updates the current admin profile.")]
    [EndpointName("UpdateCurrentAdminProfile")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdateCurrentAdminProfile([FromBody] UpdateAdminProfileRequest request, CancellationToken ct)
    {
        var command = new UpdateCurrentAdminProfileCommand(
            request.Name,
            request.Email,
            request.Phone1,
            request.Phone2);

        var result = await sender.Send(command, ct);
        return result.Match(Ok, Problem);
    }
}

public record UpdateAdminProfileRequest(
    string Name,
    string Email,
    string? Phone1,
    string? Phone2);
