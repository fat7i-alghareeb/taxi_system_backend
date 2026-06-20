using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.ResendArrivedNotification;

public class ResendArrivedNotificationCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    ITripNotifier notifier,
    INotificationService notificationService)
    : IRequestHandler<ResendArrivedNotificationCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;
    private readonly IUser _currentUser = currentUser;
    private readonly ITripNotifier _notifier = notifier;
    private readonly INotificationService _notificationService = notificationService;

    public async Task<Result<Success>> Handle(ResendArrivedNotificationCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var currentUserId))
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Auth.Unauthorized, "Unauthorized user."));
        }

        var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip == null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Trip.NotFound, "Trip not found."));
        }

        if (!_currentUser.IsAdmin)
        {
            return Error.Forbidden(LocalizationKeys.Auth.Unauthorized, "Only admins can operate trips.");
        }

        if (trip.AcceptedByAdminId != currentUserId)
        {
            return TripErrors.NotAcceptedByCurrentAdmin;
        }

        if (trip.Status != TripStatus.Arrived)
        {
            return Result.Failure<Success>(TripErrors.InvalidStatus(trip.Status));
        }

        await _notifier.NotifyDriverArrivedAsync(
            trip.Id,
            trip.PassengerId,
            trip.AcceptedByAdminId ?? Guid.Empty,
            ct);

        await _notificationService.SendPushNotificationAsync(
            trip.PassengerId,
            LocalizationKeys.Notification.DriverArrivedTitle,
            LocalizationKeys.Notification.DriverArrivedBody,
            new Dictionary<string, string>
            {
                { "tripId", trip.Id.ToString() },
                { "status", "Arrived" },
                { "eventId", Guid.NewGuid().ToString() },
                { "sound", "default" }
            },
            ct);

        return Result.Success;
    }
}
