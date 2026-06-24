using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Uploads;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.Commands.SendTripMessage;

public class SendTripMessageCommandHandler(
    IAppDbContext context,
    IFileStorage fileStorage,
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
            var validation = ImageUploadPolicy.Validate(photo.Stream, photo.FileName);
            if (validation.IsFailure)
            {
                return validation.Error;
            }

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
        message.AddDomainEvent(new TripMessageRealtimeDeliveryRequested
        {
            TripId = message.TripId,
            MessageId = message.Id,
            SenderId = message.SenderId,
            SenderRole = message.SenderRole,
            Content = message.Content,
            PhotoUrl = message.PhotoUrl,
            SentAtUtc = message.SentAtUtc,
            PassengerId = trip.PassengerId,
            DriverUserId = driverUserId,
        });
        message.AddDomainEvent(new TripMessagePushDeliveryRequested
        {
            TripId = message.TripId,
            MessageId = message.Id,
            SenderId = message.SenderId,
            SenderRole = message.SenderRole,
            Content = message.Content,
            PassengerId = trip.PassengerId,
            DriverUserId = driverUserId,
        });
        _context.TripMessages.Add(message);
        await _context.SaveChangesAsync(ct);

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
}
