using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripCompletedEventHandler(
    ITripNotifier notifier,
    INotificationService notificationService,
    IInvoiceIssuanceService invoiceIssuance,
    IAppDbContext context,
    ILogger<TripCompletedEventHandler> logger)
    : INotificationHandler<TripCompleted>
{
    private readonly ITripNotifier _notifier = notifier;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IInvoiceIssuanceService _invoiceIssuance = invoiceIssuance;
    private readonly IAppDbContext _context = context;
    private readonly ILogger<TripCompletedEventHandler> _logger = logger;

    public async Task Handle(TripCompleted notification, CancellationToken ct)
    {
        await _notifier.NotifyTripCompletedAsync(
            notification.TripId,
            notification.PassengerId,
            ct,
            notification.EventId);

        // Close the in-trip chat so both parties' inputs lock immediately. The
        // messages themselves are hard-deleted later by TripChatCleanupService.
        var driverUserId = await ResolveDriverUserIdAsync(notification.DriverId, ct);
        await _notifier.NotifyChatClosedAsync(
            notification.TripId,
            notification.PassengerId,
            driverUserId,
            ct);

        await _notificationService.SendPushNotificationAsync(
            notification.PassengerId,
            LocalizationKeys.Notification.TripCompletedTitle,
            LocalizationKeys.Notification.TripCompletedBody,
            new Dictionary<string, string>
            {
                { "tripId", notification.TripId.ToString() },
                { "status", "Completed" },
                { "type", "trip_completed" },
                { "eventId", notification.EventId.ToString() },
            },
            ct);

        try
        {
            var result = await _invoiceIssuance.EnsureIssuedAsync(notification.TripId, ct);
            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Eager invoice issuance failed for trip {TripId}: {Code} {Description}",
                    notification.TripId, result.Error.Code, result.Error.Description);
            }
        }
        catch (Exception ex)
        {
            // Never fail the trip-completion pipeline because of an invoicing hiccup.
            _logger.LogError(
                ex,
                "Failed to issue invoice for trip {TripId}; trip remains completed.",
                notification.TripId);
        }
    }

    private async Task<Guid?> ResolveDriverUserIdAsync(Guid? driverId, CancellationToken ct)
    {
        if (driverId is not { } id)
        {
            return null;
        }

        return await _context.Drivers
            .Where(d => d.Id == id)
            .Select(d => (Guid?)d.UserId)
            .FirstOrDefaultAsync(ct);
    }
}
