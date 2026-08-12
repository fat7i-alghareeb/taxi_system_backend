using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Queries.GetTripById;

public class GetTripByIdQueryHandler(
    IAppDbContext context,
    IUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<GetTripByIdQuery, Result<TripDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<TripDto>> Handle(GetTripByIdQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var trip = await _context.Trips
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct);

        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        // Authorize: the passenger, the assigned driver, or an admin. The role gate on the
        // endpoint says *which kind* of user may call this — it never says *which trip*.
        var isParticipant = currentUser.IsAdmin || trip.PassengerId == userId;
        if (!isParticipant && trip.DriverId is { } driverId)
        {
            isParticipant = await _context.Drivers
                .AsNoTracking()
                .AnyAsync(d => d.Id == driverId && d.UserId == userId, ct);
        }

        if (!isParticipant)
        {
            return TripErrors.NotATripParticipant;
        }

        return await TripDtoBuilder.BuildAsync(_context, trip, timeProvider.GetUtcNow(), ct);
    }
}
