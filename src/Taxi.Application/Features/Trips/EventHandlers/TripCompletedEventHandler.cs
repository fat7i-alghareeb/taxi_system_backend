using MediatR;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripCompletedEventHandler(
    ITripNotifier notifier,
    INotificationService notificationService,
    IInvoiceIssuanceService invoiceIssuance,
    ILogger<TripCompletedEventHandler> logger)
    : INotificationHandler<TripCompleted>
{
    private readonly ITripNotifier _notifier = notifier;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IInvoiceIssuanceService _invoiceIssuance = invoiceIssuance;
    private readonly ILogger<TripCompletedEventHandler> _logger = logger;

    public async Task Handle(TripCompleted notification, CancellationToken ct)
    {
        await _notifier.NotifyTripCompletedAsync(
            notification.TripId,
            notification.PassengerId,
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
}
