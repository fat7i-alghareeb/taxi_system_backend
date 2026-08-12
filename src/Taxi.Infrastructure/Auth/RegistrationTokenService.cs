using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Infrastructure.Auth;

/// <summary>
/// Issues/validates short-lived <c>scope=registration</c> JWTs. Google/email sign-up proves
/// email ownership first; this token carries that verified identity until the user supplies
/// the required phone in register/complete. Signed with the same JWT secret; 15-minute life.
/// </summary>
internal sealed class RegistrationTokenService(
    IConfiguration configuration,
    ILogger<RegistrationTokenService> logger) : IRegistrationTokenService
{
    private const string ScopeClaim = "scope";
    private const string ScopeValue = "registration";

    /// <summary>
    /// Dedicated audience so the JwtBearer middleware rejects a registration token outright.
    /// Sharing the access-token audience made these tokens pass <c>[Authorize]</c> — harmless
    /// today only because they carry no subject and no roles, but one added claim away from
    /// being a privilege-escalation primitive.
    /// </summary>
    private const string RegistrationAudience = "TaxiRegistration";
    private const string GoogleIdClaim = "google_id";
    private const string EmailVerifiedClaim = "email_verified";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    public string Issue(RegistrationTokenPayload payload)
    {
        var jwt = configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!));

        var claims = new List<Claim>
        {
            new(ScopeClaim, ScopeValue),
            new(JwtRegisteredClaimNames.Email, payload.Email),
            new(EmailVerifiedClaim, payload.EmailVerified ? "true" : "false"),
        };

        if (!string.IsNullOrWhiteSpace(payload.Name))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, payload.Name));
        }

        if (!string.IsNullOrWhiteSpace(payload.GoogleId))
        {
            claims.Add(new Claim(GoogleIdClaim, payload.GoogleId));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(Lifetime),
            Issuer = jwt["Issuer"],
            Audience = RegistrationAudience,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    public Result<RegistrationTokenPayload> Validate(string token)
    {
        var jwt = configuration.GetSection("JwtSettings");
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!)),
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = RegistrationAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        try
        {
            // MapInboundClaims = false keeps the raw JWT claim names ("email", "name",
            // "scope", ...). With the default (true), JwtSecurityTokenHandler renames
            // "email"/"name" to long XML-schema URIs, so FindFirstValue("email") would
            // return null and every token would be rejected as invalid.
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = handler.ValidateToken(token, parameters, out _);

            if (principal.FindFirstValue(ScopeClaim) != ScopeValue)
            {
                return AuthErrors.RegistrationTokenInvalid;
            }

            var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                return AuthErrors.RegistrationTokenInvalid;
            }

            var name = principal.FindFirstValue(JwtRegisteredClaimNames.Name);
            var googleId = principal.FindFirstValue(GoogleIdClaim);
            var emailVerified = principal.FindFirstValue(EmailVerifiedClaim) == "true";

            return new RegistrationTokenPayload(email, name, googleId, emailVerified);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Registration token validation failed.");
            return AuthErrors.RegistrationTokenInvalid;
        }
    }
}
