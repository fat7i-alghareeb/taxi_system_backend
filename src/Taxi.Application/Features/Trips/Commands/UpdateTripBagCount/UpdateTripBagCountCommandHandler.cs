using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.UpdateTripBagCount;

public class UpdateTripBagCountCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    ITripAdminEditNotifier adminEditNotifier) : IRequestHandler<UpdateTripBagCountCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(UpdateTripBagCountCommand request, CancellationToken ct)
    {
        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (!currentUser.IsAdmin)
        {
            if (!Guid.TryParse(currentUser.Id, out var passengerId))
            {
                return TripErrors.PassengerNotFound;
            }

            if (trip.PassengerId != passengerId)
            {
                return TripErrors.NotOwnedByPassenger;
            }
        }

        var updateResult = trip.UpdateBagCount(request.BagCount);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        await adminEditNotifier.NotifyAsync(trip, TripEditKind.Bags, ct);

        return Result.Success;
    }
}
