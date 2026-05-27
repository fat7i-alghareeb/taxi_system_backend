using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripRequestedEventHandler(
    ITripNotifier notifier,
    INotificationService notificationService,
    IAppDbContext context)
    : INotificationHandler<TripRequested>
{
    private readonly ITripNotifier _notifier = notifier;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IAppDbContext _context = context;

    public async Task Handle(TripRequested notification, CancellationToken ct)
    {
        await _notifier.NotifyTripRequestedAsync(
            notification.TripId,
            notification.VehicleTypeId,
            notification.PassengerId,
            ct);

        var adminIds = await _context.AdminProfiles.Select(a => a.Id).ToListAsync(ct);

        var admins = await _context.DomainUsers
            .Where(u => adminIds.Contains(u.Id) && !string.IsNullOrEmpty(u.FcmToken))
            .ToListAsync(ct);

        foreach (var admin in admins)
        {
            await _notificationService.SendPushNotificationAsync(
                admin.Id,
                LocalizationKeys.Notification.TripRequestedTitle,
                LocalizationKeys.Notification.TripRequestedBody,
                new Dictionary<string, string>
                {
                    { "tripId", notification.TripId.ToString() },
                    { "status", "TripRequested" }
                },
                ct);
        }
    }
}

