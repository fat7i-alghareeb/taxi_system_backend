using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Taxi.Application.Common.Errors;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Identity.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Identity;
using Taxi.Infrastructure.Data;

namespace Taxi.Infrastructure.Identity;

public class TokenProvider(
    IConfiguration configuration,
    AppDbContext context,
    UserManager<AppUser> userManager,
    ILogger<TokenProvider> logger) : ITokenProvider
{
    // If a client retries a refresh with a token we already rotated (its first
    // response was lost, e.g. right as the device woke from idle and reconnected),
    // replay the pair we already issued instead of failing a legitimate session.
    private static readonly TimeSpan RotationGraceWindow = TimeSpan.FromSeconds(60);

    private readonly IConfiguration configuration = configuration;
    private readonly AppDbContext context = context;
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly ILogger<TokenProvider> logger = logger;

    public async Task<Result<TokenResponse>> GenerateJwtTokenAsync(AppUserDto user, CancellationToken ct = default)
    {
        var tokenResult = await this.CreateAsync(user, ct);

        if (tokenResult.IsError)
        {
            return tokenResult.Errors;
        }

        return tokenResult.Value;
    }

    public async Task<Result<TokenResponse>> RotateAsync(RefreshToken presentedToken, AppUserDto user, CancellationToken ct = default)
    {
        var strategy = this.context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<
            (TokenProvider Provider, Guid PresentedId, AppUserDto User),
            Result<TokenResponse>>(
            state: (Provider: this, PresentedId: presentedToken.Id, User: user),
            operation: static async (_, state, token) =>
            {
                var (provider, presentedId, appUser) = state;
                var db = provider.context;

                await using var tx = await db.Database.BeginTransactionAsync(token);

                // Atomically claim the presented row: only one of two concurrent
                // requests presenting the same token can win this conditional
                // update (the row lock serializes it), scoped to that single row so
                // no other session for this user is ever touched.
                var newTokenId = Guid.NewGuid();
                var claimed = await db.RefreshTokens
                    .Where(rt => rt.Id == presentedId && rt.RevokedAtUtc == null)
                    .ExecuteUpdateAsync(
                        s => s
                            .SetProperty(rt => rt.RevokedAtUtc, DateTimeOffset.UtcNow)
                            .SetProperty(rt => rt.ReplacedByTokenId, newTokenId),
                        token);

                if (claimed == 1)
                {
                    var created = await provider.CreateAsync(appUser, token, refreshTokenId: newTokenId);

                    if (created.IsError)
                    {
                        return created.Errors;
                    }

                    await tx.CommitAsync(token);
                    return created.Value;
                }

                var current = await db.RefreshTokens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(rt => rt.Id == presentedId, token);

                if (current?.RevokedAtUtc is { } revokedAt &&
                    current.ReplacedByTokenId is { } replacementId &&
                    DateTimeOffset.UtcNow - revokedAt < RotationGraceWindow)
                {
                    var replacement = await db.RefreshTokens
                        .AsNoTracking()
                        .FirstOrDefaultAsync(rt => rt.Id == replacementId, token);

                    if (replacement is not null && replacement.ExpiresOnUtc > DateTimeOffset.UtcNow)
                    {
                        await tx.CommitAsync(token);
                        return await provider.BuildTokenResponseAsync(appUser, replacement);
                    }
                }

                // Reuse outside the grace window is the signature of a stolen token being
                // replayed: this row was already rotated long ago, yet someone still holds
                // it. Treat it as compromise and revoke every live session for the user so
                // the thief and the legitimate device are both forced to re-authenticate.
                if (current is not null && !string.IsNullOrWhiteSpace(current.UserId))
                {
                    var revoked = await db.RefreshTokens
                        .Where(rt => rt.UserId == current.UserId && rt.RevokedAtUtc == null)
                        .ExecuteUpdateAsync(
                            s => s.SetProperty(rt => rt.RevokedAtUtc, DateTimeOffset.UtcNow),
                            token);

                    provider.logger.LogWarning(
                        "[Security] Refresh-token reuse detected for user {UserId}; revoked {Count} live session(s).",
                        current.UserId,
                        revoked);
                }

                await tx.CommitAsync(token);
                return ApplicationErrors.RefreshTokenExpired;
            },
            verifySucceeded: null,
            cancellationToken: ct);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        // Returns null on any invalid token rather than throwing. Letting the
        // SecurityTokenException escape turned a bad/forged token into an unhandled 500
        // that echoed the provider's error text back to the caller.
        try
        {
            return ValidateExpiredToken(token);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private ClaimsPrincipal? ValidateExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.configuration["JwtSettings:Secret"]!)),
            ValidateIssuer = true,
            ValidIssuer = this.configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = this.configuration["JwtSettings:Audience"],
            ValidateLifetime = false, // Ignore token expiration
            ClockSkew = TimeSpan.Zero
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token.");
        }

        return principal;
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private async Task<(string AccessToken, DateTime ExpiresOnUtc, bool RequiresPasswordReset)> BuildAccessTokenAsync(AppUserDto user)
    {
        var jwtSettings = this.configuration.GetSection("JwtSettings");

        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var key = jwtSettings["Secret"]!;

        var expires = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["TokenExpirationInMinutes"]!));

        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub, user.UserId!),
            new (JwtRegisteredClaimNames.Email, user.Email!),
        };

        var dbUser = await this._userManager.FindByIdAsync(user.UserId!);
        if (dbUser != null && dbUser.RequiresPasswordReset)
        {
            claims.Add(new Claim("requires_password_reset", "true"));
        }

        foreach (var role in user.Roles)
        {
            claims.Add(new (ClaimTypes.Role, role));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256Signature),
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(descriptor);

        return (tokenHandler.WriteToken(securityToken), expires, dbUser?.RequiresPasswordReset ?? false);
    }

    private async Task<TokenResponse> BuildTokenResponseAsync(AppUserDto user, RefreshToken refreshToken)
    {
        var (accessToken, expires, requiresPasswordReset) = await this.BuildAccessTokenAsync(user);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token!,
            ExpiresOnUtc = expires,
            RequiresPasswordReset = requiresPasswordReset,
        };
    }

    /// <summary>
    /// Issues a fresh access token and inserts a brand-new refresh-token row.
    /// Used directly by login (no prior token to rotate) and by <see cref="RotateAsync"/>
    /// after it has already claimed/revoked the presented row.
    /// </summary>
    private async Task<Result<TokenResponse>> CreateAsync(AppUserDto user, CancellationToken ct = default, Guid? refreshTokenId = null)
    {
        var jwtSettings = this.configuration.GetSection("JwtSettings");

        var refreshTokenResult = RefreshToken.Create(
            refreshTokenId ?? Guid.NewGuid(),
            GenerateRefreshToken(),
            user.UserId,
            DateTime.UtcNow.AddDays(int.Parse(jwtSettings["RefreshTokenExpirationInDays"] ?? "7")));

        if (refreshTokenResult.IsError)
        {
            return refreshTokenResult.Errors;
        }

        var refreshToken = refreshTokenResult.Value;

        this.context.RefreshTokens.Add(refreshToken);

        await this.context.SaveChangesAsync(ct);

        return await this.BuildTokenResponseAsync(user, refreshToken);
    }
}
