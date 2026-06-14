using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.RateTrip;

public class RateTripCommandHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<RateTripCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(RateTripCommand request, CancellationToken ct)
    {
        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        // Only the trip's own passenger may rate it (admins excepted).
        if (!currentUser.IsAdmin)
        {
            if (!Guid.TryParse(currentUser.Id, out var passengerId))
            {
                return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
            }

            if (trip.PassengerId != passengerId)
            {
                return TripErrors.NotOwnedByPassenger;
            }
        }

        var rateResult = trip.Rate(request.Stars, request.Comment);
        if (rateResult.IsFailure)
        {
            return rateResult.Error;
        }

        await context.SaveChangesAsync(ct);
        return Result.Success;
    }
}
