using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Auth.Commands.ForceResetPassword;
using Taxi.Application.Features.Auth.Commands.Login;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.Features.Identity.Dtos;
using Taxi.Application.Features.Identity.Queries.GenerateTokens;
using Taxi.Application.Features.Identity.Queries.RefreshTokens;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(ISender sender) : ApiController
{
    [HttpPost("login")]      // Deprecated alias kept for the Blazor client; Flutter calls POST /sessions.
    [HttpPost("sessions")]   // Constitution-compliant noun (POST creates a new authenticated session).
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Verifies a Firebase phone-auth ID token and issues system JWTs.")]
    [EndpointDescription("Receives the user's phone, a Firebase ID token (proof of SMS verification), and an optional FCM device token for push notifications. Performs silent registration on first login.")]
    [EndpointName("Login")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.Match(
            this.Ok,
            this.Problem);
    }

    [HttpPost("admin/login")]       // Deprecated alias (verb + 2-level depth).
    [HttpPost("admin-sessions")]    // Constitution-compliant noun.
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Authenticates an admin using username and password.")]
    [EndpointDescription("Returns a JWT token pair. If RequiresPasswordReset is true, the admin must change their password before accessing the system.")]
    [EndpointName("AdminLogin")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> AdminLogin([FromBody] GenerateTokenQuery request, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Match(Ok, Problem);
    }

    // Constitution-compliant: password is a singleton sub-resource of /me; PUT replaces it.
    [HttpPut("me/password")]
    [Authorize]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Resets password on first login for newly provisioned accounts.")]
    [EndpointDescription("Enforces password reset for users carrying the requires_password_reset claim in their JWT.")]
    [EndpointName("ForceResetPassword")]
    [MapToApiVersion("1.0")]
    public Task<IActionResult> ForceResetPassword([FromBody] ForceResetPasswordRequest request, CancellationToken ct)
        => ForceResetPasswordCore(request, ct);

    // Deprecated legacy alias kept for the Blazor client (verb in URL).
    [HttpPost("force-reset-password")]
    [Authorize]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("[Deprecated] Use PUT /auth/me/password. Resets password on first login for newly provisioned accounts.")]
    [EndpointName("ForceResetPasswordLegacy")]
    [MapToApiVersion("1.0")]
    public Task<IActionResult> ForceResetPasswordLegacy([FromBody] ForceResetPasswordRequest request, CancellationToken ct)
        => ForceResetPasswordCore(request, ct);

    private async Task<IActionResult> ForceResetPasswordCore(ForceResetPasswordRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ForceResetPasswordCommand(request.NewPassword), ct);
        return result.Match(Ok, Problem);
    }

    // Constitution-compliant: refresh-token rotation = creating a new refresh resource.
    // Moved from IdentityController.RefreshToken (POST /identity/tokens/refresh);
    // the legacy route remains in IdentityController for the Blazor client.
    [HttpPost("tokens/refreshes")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Refreshes access token using a valid refresh token.")]
    [EndpointDescription("Exchanges an expired access token and a valid refresh token for a new token pair.")]
    [EndpointName("RefreshToken")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenQuery request, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Match(Ok, Problem);
    }
}

public record ForceResetPasswordRequest(string NewPassword);
