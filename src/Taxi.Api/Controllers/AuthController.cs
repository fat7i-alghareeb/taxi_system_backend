using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using Taxi.Application.Features.Auth.Commands.CompleteRegistration;
using Taxi.Application.Features.Auth.Commands.ContinueExistingAccount;
using Taxi.Application.Features.Auth.Commands.EmailLogin;
using Taxi.Application.Features.Auth.Commands.EmailSignup;
using Taxi.Application.Features.Auth.Commands.ForceResetPassword;
using Taxi.Application.Features.Auth.Commands.FreshStart;
using Taxi.Application.Features.Auth.Commands.Google;
using Taxi.Application.Features.Auth.Commands.Login;
using Taxi.Application.Features.Auth.Commands.PhoneLogin;
using Taxi.Application.Features.Auth.Commands.PhoneSignup;
using Taxi.Application.Features.Auth.Commands.PhoneVerify;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.Features.Identity.Dtos;
using Taxi.Application.Features.Identity.Queries.GenerateTokens;
using Taxi.Application.Features.Identity.Queries.RefreshTokens;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(ISender sender) : ApiController
{
    [HttpPost("login")] // Deprecated alias kept for the Blazor client; Flutter calls POST /sessions.
    [HttpPost("sessions")] // Constitution-compliant noun (POST creates a new authenticated session).
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Verifies a Firebase phone-auth ID token and issues system JWTs.")]
    [EndpointDescription("Receives the user's phone, a Firebase ID token (proof of SMS verification), and an optional FCM device token for push notifications. Performs silent registration on first login.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.Match(
            this.Ok,
            this.Problem);
    }

    [HttpPost("admin/login")] // Deprecated alias (verb + 2-level depth).
    [HttpPost("admin-sessions")] // Constitution-compliant noun.
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Authenticates an admin using username and password.")]
    [EndpointDescription("Returns a JWT token pair. If RequiresPasswordReset is true, the admin must change their password before accessing the system.")]
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

    // ---------------------------------------------------------------------------------
    // Backend-owned OTP auth (replaces Firebase phone verification for the mobile apps).
    // Request-OTP endpoints are throttled by the stricter "OtpRequest" limiter; the client
    // verifies with the returned otpRequestId + code.
    // ---------------------------------------------------------------------------------

    [HttpPost("phone/login/otp")]
    [AllowAnonymous]
    [EnableRateLimiting("OtpRequest")]
    [ProducesResponseType(typeof(OtpRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Phone login: sends an SMS OTP if a verified-phone account exists.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> RequestPhoneLoginOtp([FromBody] RequestPhoneLoginOtpCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command with { IpAddress = ClientIp() }, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("phone/login/otp/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Phone login: verifies the SMS OTP and returns the session.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> VerifyPhoneLoginOtp([FromBody] VerifyPhoneLoginOtpCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("phone/signup/otp")]
    [AllowAnonymous]
    [EnableRateLimiting("OtpRequest")]
    [ProducesResponseType(typeof(OtpRequestResponse), StatusCodes.Status200OK)]
    [EndpointSummary("Phone sign-up: sends an SMS OTP (existence resolved at verify time).")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> RequestPhoneSignupOtp([FromBody] RequestPhoneSignupOtpCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command with { IpAddress = ClientIp() }, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("phone/signup/otp/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Phone sign-up: verifies the SMS OTP, creating the account or flagging an existing one.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> VerifyPhoneSignupOtp([FromBody] VerifyPhoneSignupOtpCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("email/login/otp")]
    [AllowAnonymous]
    [EnableRateLimiting("OtpRequest")]
    [ProducesResponseType(typeof(OtpRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Email login: sends an email OTP if a verified-email account exists.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> RequestEmailLoginOtp([FromBody] RequestEmailLoginOtpCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command with { IpAddress = ClientIp() }, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("email/login/otp/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Email login: verifies the email OTP and returns the session.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> VerifyEmailLoginOtp([FromBody] VerifyEmailLoginOtpCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("email/signup/otp")]
    [AllowAnonymous]
    [EnableRateLimiting("OtpRequest")]
    [ProducesResponseType(typeof(OtpRequestResponse), StatusCodes.Status200OK)]
    [EndpointSummary("Email sign-up: sends an email OTP (existence resolved at verify time).")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> RequestEmailSignupOtp([FromBody] RequestEmailSignupOtpCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command with { IpAddress = ClientIp() }, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("email/signup/otp/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Email sign-up: verifies email ownership; returns a session or a registration challenge.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> VerifyEmailSignupOtp([FromBody] VerifyEmailSignupOtpCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("google")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Google sign-in/up via a verified Firebase ID token; returns a session or a registration challenge.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Google([FromBody] GoogleAuthCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("register/complete")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Finalizes a Google/email sign-up with the required (unverified) phone and returns the session.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> CompleteRegistration([FromBody] CompleteRegistrationCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("phone/verify/otp")]
    [Authorize]
    [EnableRateLimiting("OtpRequest")]
    [ProducesResponseType(typeof(OtpRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Sends an SMS OTP to verify/add a phone for the current account.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> RequestPhoneVerifyOtp([FromBody] RequestPhoneVerifyOtpCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command with { IpAddress = ClientIp() }, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("phone/verify/otp/verify")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Verifies the SMS OTP and marks the current account's phone as verified.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> VerifyPhoneVerifyOtp([FromBody] VerifyPhoneVerifyOtpCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("account/fresh-start")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [EndpointSummary("Soft-resets the current profile (keeps history for admin) and returns a refreshed session.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> FreshStart(CancellationToken ct)
    {
        var result = await sender.Send(new FreshStartCommand(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("account/continue")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Acknowledges continuing with the existing account and sends the welcome-back email.")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> ContinueExistingAccount(CancellationToken ct)
    {
        var result = await sender.Send(new ContinueExistingAccountCommand(), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private async Task<IActionResult> ForceResetPasswordCore(ForceResetPasswordRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ForceResetPasswordCommand(request.NewPassword), ct);
        return result.Match(Ok, Problem);
    }
}

public record ForceResetPasswordRequest(string NewPassword);
