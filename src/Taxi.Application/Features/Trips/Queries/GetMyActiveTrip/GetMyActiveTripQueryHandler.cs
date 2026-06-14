using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Queries.GetMyActiveTrip;

public class GetMyActiveTripQueryHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<GetMyActiveTripQuery, Result<TripDto?>>
{
    private readonly IAppDbContext _context = context;
    private readonly IUser _currentUser = currentUser;

    // Non-terminal statuses that count as a live, resumable trip. AwaitingPayment
    // and PendingQuote are excluded — those are still part of the booking flow.
    private static readonly TripStatus[] ActiveStatuses =
    [
        TripStatus.Scheduled,
        TripStatus.PendingDriver,
        TripStatus.DriverAssigned,
        TripStatus.DriverEnRoute,
        TripStatus.DriverArrived,
        TripStatus.InProgress,
    ];

    public async Task<Result<TripDto?>> Handle(GetMyActiveTripQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        // A user may be a passenger and/or a driver; prefer a trip they are riding,
        // then fall back to one they are driving.
        var trip = await _context.Trips
            .Where(t => t.PassengerId == userId && ActiveStatuses.Contains(t.Status))
            .OrderByDescending(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (trip is null)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId, ct);
            if (driver is not null)
            {
                trip = await _context.Trips
                    .Where(t => t.DriverId == driver.Id && ActiveStatuses.Contains(t.Status))
                    .OrderByDescending(t => t.CreatedAtUtc)
                    .FirstOrDefaultAsync(ct);
            }
        }

        if (trip is null)
        {
            return (TripDto?)null;
        }

        return await TripDtoBuilder.BuildAsync(_context, trip, ct);
    }
}
