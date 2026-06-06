using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Admins.Commands.UpdateCurrentAdminProfile;
using Taxi.Application.Features.Admins.Dtos;
using Taxi.Application.Features.Admins.Queries.GetCurrentAdminProfile;
using Taxi.Application.Features.Identity.Commands.RegisterAdmin;
using Taxi.Contracts.Requests.Identity;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
[Route("api/v{version:apiVersion}/admins")]
public sealed class AdminsController(ISender sender) : ApiController
{
    // Constitution-compliant: POST /admins creates a new admin resource.
    // Moved from IdentityController.RegisterAdmin (POST /identity/admins);
    // the legacy route remains in IdentityController for the Blazor client.
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Creates a new administrative user.")]
    [EndpointDescription("Only existing Admins can create new Admins.")]
    [EndpointName("RegisterAdmin")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminRequest request, CancellationToken ct)
    {
        var command = new RegisterAdminCommand(
            request.UserName,
            request.Password,
            request.Name,
            request.Email,
            request.Phone1,
            request.Phone2);

        var result = await sender.Send(command, ct);
        return result.Match(id => Ok(id), Problem);
    }

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

    // Constitution-compliant: password is a singleton sub-resource of /me, PUT replaces it.
    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Changes the password of the currently authenticated admin.")]
    [EndpointName("ChangeAdminPassword")]
    [MapToApiVersion("1.0")]
    public Task<IActionResult> ChangeAdminPassword([FromBody] ChangeAdminPasswordRequest request, CancellationToken ct)
        => ChangeAdminPasswordCore(request, ct);

    // Deprecated legacy alias kept for the Blazor client (verb in URL).
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("[Deprecated] Use PUT /me/password. Changes the password of the currently authenticated admin.")]
    [EndpointName("ChangeAdminPasswordLegacy")]
    [MapToApiVersion("1.0")]
    public Task<IActionResult> ChangeAdminPasswordLegacy([FromBody] ChangeAdminPasswordRequest request, CancellationToken ct)
        => ChangeAdminPasswordCore(request, ct);

    private async Task<IActionResult> ChangeAdminPasswordCore(ChangeAdminPasswordRequest request, CancellationToken ct)
    {
        var command = new Taxi.Application.Features.Admins.Commands.ChangeAdminPassword.ChangeAdminPasswordCommand(
            request.CurrentPassword,
            request.NewPassword);

        var result = await sender.Send(command, ct);
        return result.Match(_ => Ok(), Problem);
    }
}

public record UpdateAdminProfileRequest(
    string Name,
    string Email,
    string? Phone1,
    string? Phone2);

public record ChangeAdminPasswordRequest(
    string CurrentPassword,
    string NewPassword);
