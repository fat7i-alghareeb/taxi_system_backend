using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Taxi.Application.Features.Auth.Commands.SendOtp;
using Taxi.Application.Features.Auth.Commands.VerifyOtp;
using Taxi.Application.Features.Auth.Dtos;

namespace Taxi.Api.Controllers;

[AllowAnonymous]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(ISender sender) : ApiController
{
    [HttpPost("send-otp")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Sends a 4-digit OTP to the provided phone number.")]
    [EndpointDescription("Triggers the SMS provider and returns a temporary session token.")]
    [EndpointName("SendOtp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.Match(
            sessionToken => this.Ok(new { sessionToken }),
            this.Problem);
    }

    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Verifies the OTP and issues authentication tokens.")]
    [EndpointDescription("Validates the code against the session. Performs silent registration if the user is new.")]
    [EndpointName("VerifyOtp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.Match(
            this.Ok,
            this.Problem);
    }
}
