using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Queries.GetMyActiveTrips;

public class GetMyActiveTripsQueryHandler(
    IAppDbContext context,
    IUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<GetMyActiveTripsQuery, Result<List<TripDto>>>
{
    private readonly IAppDbContext _context = context;
    private readonly IUser _currentUser = currentUser;

    private static readonly TripStatus[] ActiveStatuses = TripStatuses.Active;

    /// Sanity cap. A passenger legitimately holding more than this many open trips
    /// is a support case, not a UI case.
    private const int MaxTrips = 20;

    public async Task<Result<List<TripDto>>> Handle(GetMyActiveTripsQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        // Passenger leg only: this endpoint backs the customer app's booking gate.
        // Driver/admin assignment resolution stays in GetMyActiveTripQuery.
        var trips = await _context.Trips
            .AsNoTracking()
            .Where(t => t.PassengerId == userId && ActiveStatuses.Contains(t.Status))
            // Soonest pickup first; immediate trips (no schedule) sort by creation.
            .OrderBy(t => t.ScheduledAtUtc ?? t.CreatedAtUtc)
            .Take(MaxTrips)
            .ToListAsync(ct);

        var now = timeProvider.GetUtcNow();
        var dtos = new List<TripDto>(trips.Count);
        foreach (var trip in trips)
        {
            dtos.Add(await TripDtoBuilder.BuildAsync(_context, trip, now, ct));
        }

        return dtos;
    }
}
