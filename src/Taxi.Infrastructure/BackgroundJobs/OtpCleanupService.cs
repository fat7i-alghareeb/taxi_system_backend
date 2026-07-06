using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;

namespace Taxi.Infrastructure.BackgroundJobs;

/// <summary>
/// Periodically deletes expired/consumed OTP rows older than the configured retention
/// window so the table never grows unbounded, while recent rows remain available for
/// debugging / rate-limit / audit.
/// </summary>
public sealed class OtpCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<OtpOptions> options,
    ILogger<OtpCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    private readonly int _retentionDays = options.Value.OtpRetentionDays;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OtpCleanupService started (retention {Days}d).", _retentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "OtpCleanupService pass failed.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var threshold = DateTimeOffset.UtcNow.AddDays(-_retentionDays);

        var removed = await context.OtpCodes
            .Where(o => (o.ConsumedAtUtc != null || o.ExpiresAtUtc < DateTimeOffset.UtcNow)
                && o.CreatedAtUtc < threshold)
            .ExecuteDeleteAsync(ct);

        if (removed > 0)
        {
            logger.LogInformation("OtpCleanupService removed {Count} stale OTP row(s).", removed);
        }
    }
}
