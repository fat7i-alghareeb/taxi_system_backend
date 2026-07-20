using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Common;
using Taxi.Contracts.Common;
using Taxi.Contracts.Notifications;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.CompleteTrip;

public class CompleteTripCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IClientConfigProvider clientConfig,
    IFeeSettlementService feeSettlement,
    INotificationService notifications,
    ITripNotifier tripNotifier,
    TimeProvider timeProvider,
    ILogger<CompleteTripCommandHandler> logger)
    : IRequestHandler<CompleteTripCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(CompleteTripCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var adminUserId))
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Auth.Unauthorized, "Unauthorized user."));
        }

        var trip = await _context.Trips
            .Include(t => t.Stops)
            .FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip == null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Trip.NotFound, "Trip not found."));
        }

        if (!currentUser.IsAdmin)
        {
            return Error.Forbidden(LocalizationKeys.Auth.Unauthorized, "Only admins can operate trips.");
        }

        if (trip.AcceptedByAdminId != adminUserId)
        {
            return await TripOwnershipHelper.NotOwnedByCurrentAdminAsync(_context, trip.AcceptedByAdminId, ct);
        }

        // Stop any open waiting meter so its accrued fee is finalized. Load every session for the
        // trip up front and sum in memory: a trip can accrue more than one waiting session (wait,
        // drive to a stop, wait again), and the just-stopped one is not yet persisted, so a
        // database-side SUM would miss it. Charging only the open session under-billed and left
        // the invoice with a remaining balance.
        var now = timeProvider.GetUtcNow();
        var waitingSessions = await _context.TripWaitingSessions
            .Where(s => s.TripId == trip.Id)
            .ToListAsync(ct);
        waitingSessions.FirstOrDefault(s => s.StoppedAtUtc == null)?.Stop(now);
        var waitingFeeTotal = waitingSessions.Sum(s => s.EstimatedFee ?? 0m);

        // Settle the waiting-fee surcharge BEFORE completing. The invoice is issued asynchronously
        // by the TripCompleted domain-event handler; settling first guarantees the WaitingFee
        // payment row is committed before that event, so the invoice's paid/remaining totals
        // reflect the collected fee instead of snapshotting it as outstanding. Best-effort — a
        // settlement hiccup must never block completion.
        await TrySettleWaitingFeeAsync(trip, waitingFeeTotal, ct);

        var transitionResult = trip.Complete(now);
        if (transitionResult.IsFailure)
        {
            return transitionResult.Error;
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success;
    }

    // Settles the accrued waiting fee via the fee engine (wallet first, then the default saved
    // reusable card, else Unpaid). Best-effort — never fails the completion. Notifies the
    // passenger if any portion could not be collected.
    private async Task TrySettleWaitingFeeAsync(Trip trip, decimal fee, CancellationToken ct)
    {
        if (fee <= 0m)
        {
            return;
        }

        // No StripeEnabled gate here: only the card step needs Stripe, and it self-gates inside
        // the settlement service. The wallet and debt steps must still run when Stripe is off, or
        // the fee vanishes and the invoice snapshots a "Remaining due" line.
        var currency = await _context.Payments
            .Where(p => p.TripId == trip.Id && p.Kind == PaymentKind.Fare)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => p.Currency)
            .FirstOrDefaultAsync(ct) ?? "EUR";

        try
        {
            var outcome = await feeSettlement.SettleWaitingFeeAsync(
                trip.Id, trip.PassengerId, fee, currency, $"waiting-fee-{trip.Id}", ct);

            if (outcome.IsSuccess && outcome.Value.HasDebt)
            {
                // Moved to the customer's wallet as debt — they must clear it before booking
                // again, so point them at the wallet rather than a per-trip payment sheet.
                await NotifyWalletDebtAsync(trip, outcome.Value.ChargedToDebt, currency, ct);
                await NotifyBalanceChangedAsync(trip.PassengerId, currency, ct);
            }
            else if (outcome.IsSuccess && outcome.Value.HasUnpaid)
            {
                await NotifyWaitingFeeDueAsync(trip, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to settle waiting fee for trip {TripId}.", trip.Id);
        }
    }

    /// <summary>
    /// Pushes the new balance over realtime so an open app shows the debt (and the resulting
    /// booking block) at once, rather than only when the customer next opens the wallet screen.
    /// </summary>
    private async Task NotifyBalanceChangedAsync(Guid passengerId, string currency, CancellationToken ct)
    {
        try
        {
            var account = await _context.WalletAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == passengerId, ct);
            if (account is null)
            {
                return;
            }

            await tripNotifier.NotifyWalletBalanceChangedAsync(
                new WalletBalanceChangedNotification(
                    passengerId,
                    account.Balance,
                    account.AmountOwed,
                    account.Currency,
                    Guid.NewGuid()),
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast wallet balance for passenger {PassengerId}.", passengerId);
        }
    }

    private async Task NotifyWalletDebtAsync(Trip trip, decimal amount, string currency, CancellationToken ct)
    {
        try
        {
            await notifications.SendPushNotificationAsync(
                trip.PassengerId,
                LocalizationKeys.Notification.WalletDebtTitle,
                LocalizationKeys.Notification.WalletDebtBody,
                new Dictionary<string, string>
                {
                    { "tripId", trip.Id.ToString() },
                    { "type", "wallet_debt" },
                },
                ct,
                bodyArgs: [$"{amount:0.00} {currency}"]);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to notify passenger of wallet debt for trip {TripId}.", trip.Id);
        }
    }

    private async Task NotifyWaitingFeeDueAsync(Trip trip, CancellationToken ct)
    {
        try
        {
            await notifications.SendPushNotificationAsync(
                trip.PassengerId,
                LocalizationKeys.Notification.WaitingFeeDueTitle,
                LocalizationKeys.Notification.WaitingFeeDueBody,
                new Dictionary<string, string>
                {
                    { "tripId", trip.Id.ToString() },
                    { "type", "waiting_fee_due" },
                },
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to notify passenger of outstanding waiting fee for trip {TripId}.", trip.Id);
        }
    }
}
