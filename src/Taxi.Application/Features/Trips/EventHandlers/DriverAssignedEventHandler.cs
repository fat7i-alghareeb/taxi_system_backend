using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class DriverAssignedEventHandler(ITripNotifier notifier)
    : INotificationHandler<DriverAssigned>
{
    private readonly ITripNotifier _notifier = notifier;

    public Task Handle(DriverAssigned notification, CancellationToken ct) =>
        _notifier.NotifyDriverAssignedAsync(
            notification.TripId,
            notification.PassengerId,
            notification.DriverId,
            ct);
}
