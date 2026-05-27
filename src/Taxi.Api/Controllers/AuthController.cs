using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Auth.Commands.ForceResetPassword;
using Taxi.Application.Features.Auth.Commands.Login;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.Features.Identity.Dtos;
using Taxi.Application.Features.Identity.Queries.GenerateTokens;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(ISender sender) : ApiController
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Verifies a Firebase phone-auth ID token and issues system JWTs.")]
    [EndpointDescription("Receives the user's phone, a Firebase ID token (proof of SMS verification), and an optional FCM device token for push notifications. Performs silent registration on first login.")]
    [EndpointName("Login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.Match(
            this.Ok,
            this.Problem);
    }

    [HttpPost("admin/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Authenticates an admin using username and password.")]
    [EndpointDescription("Returns a JWT token pair. If RequiresPasswordReset is true, the admin must change their password before accessing the system.")]
    [EndpointName("AdminLogin")]
    public async Task<IActionResult> AdminLogin([FromBody] GenerateTokenQuery request, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("force-reset-password")]
    [Authorize]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Resets password on first login for newly provisioned accounts.")]
    [EndpointDescription("Enforces password reset for users carrying the requires_password_reset claim in their JWT.")]
    [EndpointName("ForceResetPassword")]
    public async Task<IActionResult> ForceResetPassword([FromBody] ForceResetPasswordRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ForceResetPasswordCommand(request.NewPassword), ct);
        return result.Match(Ok, Problem);
    }
}

public record ForceResetPasswordRequest(string NewPassword);
