using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Infrastructure.BackgroundJobs;

/// <summary>
/// Reverts mid-trip fare-increase edits whose interactive PaymentSheet was abandoned (no
/// success/failure webhook arrived) once the held quote has expired: cancels the pending edit,
/// releases the orphan quote, fails the pending delta payment, cancels the Stripe intent, and
/// reverses any wallet portion. The trip was never mutated, so this simply lets the change lapse.
/// </summary>
public sealed class PendingTripEditExpiryService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<PendingTripEditExpiryService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Pending trip-edit expiry service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireDueEditsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Pending trip-edit expiry pass failed; retrying next tick.");
            }

            await Task.Delay(CheckInterval, timeProvider, stoppingToken);
        }
    }

    private async Task ExpireDueEditsAsync(CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();

        List<Guid> dueEditIds;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            dueEditIds = await context.PendingTripEdits
                .Where(e => e.Status == PendingTripEditStatus.Pending && e.ExpiresAtUtc <= now)
                .Select(e => e.Id)
                .ToListAsync(ct);
        }

        foreach (var editId in dueEditIds)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var refundService = scope.ServiceProvider.GetRequiredService<IRefundLifecycleService>();
            var stripe = scope.ServiceProvider.GetRequiredService<IStripePaymentService>();

            var edit = await context.PendingTripEdits.FirstOrDefaultAsync(e => e.Id == editId, ct);
            if (edit is null || edit.Status != PendingTripEditStatus.Pending)
            {
                continue;
            }

            // Release the orphan quote.
            var quote = await context.PricingQuotes.FirstOrDefaultAsync(q => q.Id == edit.NewQuoteId, ct);
            quote?.MarkAsUnused();

            // Fail the pending delta payment so it never lingers Pending.
            var payment = await context.Payments
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == edit.StripePaymentIntentId, ct);
            if (payment is not null && payment.Status == PaymentStatus.Pending)
            {
                payment.MarkAsFailed("edit_expired", "Fare-adjustment PaymentSheet abandoned.");
            }

            edit.MarkExpired();

            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogInformation(
                    "Skipped expiring pending edit {EditId}; it was modified concurrently (likely just paid).",
                    editId);
                continue;
            }

            // Cancel the Stripe intent so a late sheet confirmation can't charge after expiry (best-effort).
            await stripe.CancelPaymentIntentAsync(edit.StripePaymentIntentId, ct);

            // Reverse any wallet portion already collected toward this delta.
            if (edit.WalletPaymentId is Guid walletPaymentId && edit.WalletDebitedAmount > 0m)
            {
                var refundRequest = new RefundRequest(
                    walletPaymentId,
                    edit.WalletDebitedAmount,
                    PaymentRefundSourceType.FareAdjustment,
                    TripId: edit.TripId,
                    PassengerId: edit.PassengerId);
                await refundService.RequestRefundAsync(refundRequest, ct);
            }

            logger.LogInformation("Expired abandoned pending trip-edit {EditId} for trip {TripId}.", editId, edit.TripId);
        }
    }
}
