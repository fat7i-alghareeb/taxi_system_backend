using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.UpdateTripScheduledTime;

public class UpdateTripScheduledTimeCommandHandler(
    IAppDbContext context,
    IUser currentUser) : IRequestHandler<UpdateTripScheduledTimeCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(UpdateTripScheduledTimeCommand request, CancellationToken ct)
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

        // This endpoint bypasses the preview/apply pipeline, so the customer edit window has to be
        // enforced here — same as UpdateTripBagCountCommandHandler. Deliberately after the
        // ownership check, so a stranger cannot probe a trip's booking time.
        if (TripEditPolicy.Validate(
                trip,
                editsStops: false,
                editsPartySize: false,
                editsSchedule: true) is { } policyError)
        {
            return policyError;
        }

        var updateResult = trip.UpdateScheduledTime(request.ScheduledAtUtc);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await context.SaveChangesAsync(ct);
        return Result.Success;
    }
}
