using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Common;
using Taxi.Contracts.Common;
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

        // Settle any open waiting meter first so its accrued fee is computed and
        // visible (via the tracked entity) to the invoice that the TripCompleted
        // domain-event handler issues during SaveChangesAsync.
        var now = timeProvider.GetUtcNow();
        var activeWaiting = await _context.TripWaitingSessions
            .FirstOrDefaultAsync(s => s.TripId == trip.Id && s.StoppedAtUtc == null, ct);
        activeWaiting?.Stop(now);

        var transitionResult = trip.Complete(now);
        if (transitionResult.IsFailure)
        {
            return transitionResult.Error;
        }

        await _context.SaveChangesAsync(ct);

        // The trip is now committed; collecting the waiting-fee surcharge is a
        // best-effort side effect that must never fail the completion itself.
        await TrySettleWaitingFeeAsync(trip, activeWaiting?.EstimatedFee ?? 0m, ct);

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

        if (!clientConfig.GetClientConfig().StripeEnabled)
        {
            return;
        }

        var currency = await _context.Payments
            .Where(p => p.TripId == trip.Id && p.Kind == PaymentKind.Fare)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => p.Currency)
            .FirstOrDefaultAsync(ct) ?? "EUR";

        try
        {
            var outcome = await feeSettlement.SettleWaitingFeeAsync(
                trip.Id, trip.PassengerId, fee, currency, $"waiting-fee-{trip.Id}", ct);

            if (outcome.IsSuccess && outcome.Value.HasUnpaid)
            {
                await NotifyWaitingFeeDueAsync(trip, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to settle waiting fee for trip {TripId}.", trip.Id);
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
