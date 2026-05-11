using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Taxi.Application.Features.Auth.Commands.Login;
using Taxi.Application.Features.Auth.Dtos;

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
}
