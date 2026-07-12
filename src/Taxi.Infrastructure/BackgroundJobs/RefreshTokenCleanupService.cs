using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;

namespace Taxi.Infrastructure.BackgroundJobs;

/// <summary>
/// Periodically deletes revoked/expired refresh-token rows so the table doesn't grow
/// unbounded now that rotation revokes one row at a time instead of the old
/// delete-everything-on-every-login behavior.
/// </summary>
public sealed class RefreshTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<RefreshTokenCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan RevokedRetention = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RefreshTokenCleanupService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "RefreshTokenCleanupService pass failed.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var revokedThreshold = DateTimeOffset.UtcNow.Subtract(RevokedRetention);

        var removed = await context.RefreshTokens
            .Where(rt => rt.ExpiresOnUtc < DateTimeOffset.UtcNow
                || (rt.RevokedAtUtc != null && rt.RevokedAtUtc < revokedThreshold))
            .ExecuteDeleteAsync(ct);

        if (removed > 0)
        {
            logger.LogInformation("RefreshTokenCleanupService removed {Count} stale refresh token row(s).", removed);
        }
    }
}
