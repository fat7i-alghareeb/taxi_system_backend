using System.Security.Claims;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Identity.Commands.RegisterAdmin;
using Taxi.Application.Features.Identity.Dtos;
using Taxi.Application.Features.Identity.Queries.GetUserInfo;
using Taxi.Application.Features.Identity.Queries.RefreshTokens;
using Taxi.Contracts.Requests.Identity;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/identity")]
public sealed class IdentityController(ISender sender) : ApiController
{
    [HttpPost("tokens/refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("[Deprecated] Use POST /api/v1/auth/tokens/refreshes instead. Refreshes access token using a valid refresh token.")]
    [EndpointDescription("Exchanges an expired access token and a valid refresh token for a new token pair. This route is kept as an alias for the Blazor client; Flutter calls the noun-modeled equivalent in AuthController.")]
    [EndpointName("RefreshTokenLegacy")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenQuery request, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Match(
            response => this.Ok(response),
            this.Problem);
    }

    [HttpGet("current-user/claims")]
    [Authorize]
    [ProducesResponseType(typeof(AppUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("[Deprecated] Use GET /api/v1/users/me/claims instead. Gets the current authenticated user's info.")]
    [EndpointDescription("Returns user information for the currently authenticated user based on the access token. This route is kept as an alias for the Blazor client; Flutter calls the noun-modeled equivalent in UsersController.")]
    [EndpointName("GetCurrentUserClaimsLegacy")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetCurrentUserInfo(CancellationToken ct)
    {
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return this.Unauthorized();
        }

        var result = await sender.Send(new GetUserByIdQuery(userId), ct);

        return result.Match(
            response => this.Ok(response),
            this.Problem);
    }

    [HttpPost("admins")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("[Deprecated] Use POST /api/v1/admins instead. Creates a new administrative user.")]
    [EndpointDescription("Only existing Admins can create new Admins. This route is kept as an alias for the Blazor client; Flutter calls the canonical equivalent in AdminsController.")]
    [EndpointName("RegisterAdminLegacy")]
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
        return result.Match(
            response => this.Ok(response),
            this.Problem);
    }
}


