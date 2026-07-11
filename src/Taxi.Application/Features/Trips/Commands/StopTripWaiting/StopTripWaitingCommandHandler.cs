using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Common;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.StopTripWaiting;

public sealed class StopTripWaitingCommandHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<StopTripWaitingCommand, Result<WaitingSessionDto>>
{
    public async Task<Result<WaitingSessionDto>> Handle(StopTripWaitingCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var driverUserId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        if (!currentUser.IsAdmin)
        {
            return Error.Forbidden(LocalizationKeys.Auth.Unauthorized, "Only admins can operate trips.");
        }

        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        // Authorize by the operating admin, not by the session's DriverId: when a real driver is
        // assigned, the session's DriverId is the driver's id (set on auto-start at arrival), not
        // the admin's user id — matching on it would miss the open session and 404.
        if (trip.AcceptedByAdminId != driverUserId)
        {
            return await TripOwnershipHelper.NotOwnedByCurrentAdminAsync(context, trip.AcceptedByAdminId, ct);
        }

        var session = await context.TripWaitingSessions.FirstOrDefaultAsync(
            s => s.TripId == request.TripId && s.StoppedAtUtc == null,
            ct);
        if (session is null)
        {
            return TripErrors.ActiveWaitingSessionNotFound;
        }

        var stopResult = session.Stop();
        if (stopResult.IsError)
        {
            return stopResult.Errors;
        }

        await context.SaveChangesAsync(ct);
        return session.ToDto();
    }
}
