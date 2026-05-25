using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Auth.Commands.Login;
using Taxi.Application.Features.Auth.Commands.ForceResetPassword;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.Features.Identity.Dtos;

namespace Taxi.Api.Controllers;

[AllowAnonymous]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(ISender sender) : ApiController
{
    [HttpPost("login")]
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
