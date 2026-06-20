using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Contracts.Notifications;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.SendTripMessage;

public class SendTripMessageCommandHandler(
    IAppDbContext context,
    IFileStorage fileStorage,
    ITripNotifier notifier,
    INotificationService notificationService,
    IUser currentUser)
    : IRequestHandler<SendTripMessageCommand, Result<TripMessageDto>>
{
    // Statuses in which the chat is no longer available.
    private static readonly TripStatus[] ChatClosedStatuses =
    [
        TripStatus.Completed,
        TripStatus.Cancelled,
        TripStatus.Refunded,
        TripStatus.PaymentFailed,
    ];

    private readonly IAppDbContext _context = context;
    private readonly IFileStorage _fileStorage = fileStorage;
    private readonly ITripNotifier _notifier = notifier;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IUser _currentUser = currentUser;

    public async Task<Result<TripMessageDto>> Handle(SendTripMessageCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var senderId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (ChatClosedStatuses.Contains(trip.Status))
        {
            return TripErrors.ChatClosed;
        }

        var driverUserId = await ResolveDriverUserIdAsync(trip.DriverId, ct);

        // Resolve the sender's role and authorize: passenger, assigned driver, or admin.
        TripMessageSenderRole senderRole;
        if (senderId == trip.PassengerId)
        {
            senderRole = TripMessageSenderRole.Passenger;
        }
        else if (driverUserId is { } du && du == senderId)
        {
            senderRole = TripMessageSenderRole.Driver;
        }
        else if (_currentUser.IsAdmin)
        {
            senderRole = TripMessageSenderRole.Admin;
        }
        else
        {
            return TripErrors.NotAChatParticipant;
        }

        // Persist the photo (if any) before constructing the message.
        string? photoUrl = null;
        if (request.Photo is { } photo)
        {
            var extension = Path.GetExtension(photo.FileName);
            photoUrl = await _fileStorage.SaveAsync(
                photo.Stream,
                $"chat/trips/{trip.Id:N}/{Guid.NewGuid():N}{extension}",
                ct);
        }

        var messageResult = TripMessage.Create(
            Guid.NewGuid(),
            trip.Id,
            senderId,
            senderRole,
            request.Content,
            photoUrl);

        if (messageResult.IsFailure)
        {
            return messageResult.Error;
        }

        var message = messageResult.Value;
        _context.TripMessages.Add(message);
        await _context.SaveChangesAsync(ct);

        var notification = new TripMessageNotification(
            message.TripId,
            message.Id,
            message.SenderId,
            message.SenderRole.ToString(),
            message.Content,
            message.PhotoUrl,
            message.SentAtUtc);

        await _notifier.NotifyTripMessageAsync(notification, trip.PassengerId, driverUserId, ct);

        await PushToRecipientsAsync(message, trip.PassengerId, driverUserId, senderId, ct);

        return new TripMessageDto(
            message.Id,
            message.TripId,
            message.SenderId,
            message.SenderRole.ToString(),
            message.Content,
            message.PhotoUrl,
            message.SentAtUtc);
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

    /// <summary>
    /// Sends a push notification to the trip's other party (passenger and/or driver),
    /// never to the sender. Admins receive only the live SignalR update.
    /// </summary>
    private async Task PushToRecipientsAsync(
        TripMessage message,
        Guid passengerId,
        Guid? driverUserId,
        Guid senderId,
        CancellationToken ct)
    {
        var body = string.IsNullOrWhiteSpace(message.Content)
            ? LocalizationKeys.Notification.NewPhotoMessageBody
            : message.Content;

        var data = new Dictionary<string, string>
        {
            { "type", "chat_message" },
            { "tripId", message.TripId.ToString() },
            { "messageId", message.Id.ToString() },
        };

        var recipients = new HashSet<Guid> { passengerId };
        if (driverUserId is { } du)
        {
            recipients.Add(du);
        }

        recipients.Remove(senderId);

        foreach (var recipient in recipients)
        {
            await _notificationService.SendPushNotificationAsync(
                recipient,
                LocalizationKeys.Notification.NewMessageTitle,
                body,
                data,
                ct);
        }

        // When a passenger messages, alert EVERY admin via the shared "admins"
        // topic — regardless of whether a driver is assigned, who owns the trip,
        // or the trip status (e.g. PendingDriver). Admins also get the live
        // SignalR event, but the push reaches them even with no chat open.
        if (message.SenderRole == TripMessageSenderRole.Passenger)
        {
            await _notificationService.SendPushNotificationToTopicAsync(
                NotificationTopics.Admins,
                LocalizationKeys.Notification.NewMessageTitle,
                body,
                data,
                ct);
        }
    }
}
