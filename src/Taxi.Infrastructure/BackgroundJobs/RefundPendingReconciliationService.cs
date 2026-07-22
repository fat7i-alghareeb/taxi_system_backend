using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;
using Taxi.Domain.Payments;

namespace Taxi.Infrastructure.BackgroundJobs;

/// <summary>
/// Periodically finds refunds stuck in Pending — accepted by Stripe but never finalized
/// because their confirming webhook was never delivered or processed — and reconciles
/// each one directly against Stripe, so a refund can never sit unresolved forever with
/// no admin visibility into why.
/// </summary>
public sealed class RefundPendingReconciliationService(
    IServiceScopeFactory scopeFactory,
    IOptions<RefundReconciliationOptions> options,
    ILogger<RefundPendingReconciliationService> logger) : BackgroundService
{
    private readonly RefundReconciliationOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromMinutes(_options.PollIntervalMinutes);
        logger.LogInformation(
            "RefundPendingReconciliationService started (poll every {PollMinutes}m, stuck threshold {ThresholdMinutes}m).",
            _options.PollIntervalMinutes,
            _options.StuckPendingThresholdMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcileStuckRefundsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "RefundPendingReconciliationService pass failed.");
            }

            await Task.Delay(pollInterval, stoppingToken);
        }
    }

    private async Task ReconcileStuckRefundsAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var threshold = DateTimeOffset.UtcNow.AddMinutes(-_options.StuckPendingThresholdMinutes);

        var stuckRefundIds = await context.PaymentRefunds
            .Where(refund => refund.Status == PaymentRefundStatus.Pending &&
                (refund.LastReconciledAtUtc ?? refund.LastAttemptAtUtc ?? refund.RequestedAtUtc) < threshold)
            .Select(refund => refund.Id)
            .ToListAsync(ct);

        if (stuckRefundIds.Count == 0)
        {
            return;
        }

        logger.LogInformation("RefundPendingReconciliationService found {Count} stuck pending refund(s).", stuckRefundIds.Count);

        foreach (var refundId in stuckRefundIds)
        {
            try
            {
                await using var itemScope = scopeFactory.CreateAsyncScope();
                var refundService = itemScope.ServiceProvider.GetRequiredService<IRefundLifecycleService>();
                await refundService.ReconcilePendingRefundAsync(refundId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to reconcile refund {RefundId}.", refundId);
            }
        }
    }
}
