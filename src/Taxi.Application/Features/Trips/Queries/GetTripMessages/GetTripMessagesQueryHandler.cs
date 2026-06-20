using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Queries.GetTripMessages;

public class GetTripMessagesQueryHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<GetTripMessagesQuery, Result<IReadOnlyList<TripMessageDto>>>
{
    private readonly IAppDbContext _context = context;
    private readonly IUser _currentUser = currentUser;

    public async Task<Result<IReadOnlyList<TripMessageDto>>> Handle(GetTripMessagesQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var trip = await _context.Trips
            .Where(t => t.Id == request.TripId)
            .Select(t => new { t.PassengerId, t.DriverId })
            .FirstOrDefaultAsync(ct);

        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        // Authorize: passenger, the assigned driver, or an admin may read the chat.
        var isParticipant = trip.PassengerId == userId || _currentUser.IsAdmin;
        if (!isParticipant && trip.DriverId is { } driverId)
        {
            isParticipant = await _context.Drivers
                .AnyAsync(d => d.Id == driverId && d.UserId == userId, ct);
        }

        if (!isParticipant)
        {
            return TripErrors.NotAChatParticipant;
        }

        var messages = await _context.TripMessages
            .Where(m => m.TripId == request.TripId)
            .OrderBy(m => m.SentAtUtc)
            .ToListAsync(ct);

        return messages
            .Select(m => new TripMessageDto(
                m.Id,
                m.TripId,
                m.SenderId,
                m.SenderRole.ToString(),
                m.Content,
                m.PhotoUrl,
                m.SentAtUtc))
            .ToList();
    }
}
