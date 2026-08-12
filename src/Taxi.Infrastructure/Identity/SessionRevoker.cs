using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Infrastructure.Data;

namespace Taxi.Infrastructure.Identity;

/// <inheritdoc cref="ISessionRevoker"/>
internal sealed class SessionRevoker(AppDbContext context, ILogger<SessionRevoker> logger) : ISessionRevoker
{
    public async Task<int> RevokeAllForUserAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return 0;
        }

        var revoked = await context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAtUtc, DateTimeOffset.UtcNow), ct);

        if (revoked > 0)
        {
            logger.LogInformation(
                "[Security] Revoked {Count} refresh token(s) for user {UserId}",
                revoked,
                userId);
        }

        return revoked;
    }

    public async Task<bool> RevokeTokenAsync(string userId, string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        var revoked = await context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.Token == refreshToken && rt.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAtUtc, DateTimeOffset.UtcNow), ct);

        return revoked > 0;
    }
}
