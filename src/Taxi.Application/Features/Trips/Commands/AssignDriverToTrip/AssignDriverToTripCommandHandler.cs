using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.AssignDriverToTrip;

/// <summary>
/// Compatibility handler for the legacy assignment route. In the admin-only
/// workflow the current admin accepts the trip; the driver id is ignored.
/// </summary>
public class AssignDriverToTripCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<AssignDriverToTripCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(AssignDriverToTripCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var adminUserId) || !currentUser.IsAdmin)
        {
            return Error.Unauthorized(LocalizationKeys.Auth.Unauthorized, "Unauthorized user.");
        }

        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        var acceptResult = trip.AcceptByAdmin(adminUserId, timeProvider.GetUtcNow());
        if (acceptResult.IsFailure)
        {
            return acceptResult.Error;
        }

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TripErrors.AlreadyAccepted;
        }

        return Result.Success;
    }
}
