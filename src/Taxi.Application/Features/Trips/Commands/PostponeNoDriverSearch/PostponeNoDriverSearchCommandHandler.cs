using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.PostponeNoDriverSearch;

public class PostponeNoDriverSearchCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<PostponeNoDriverSearchCommand, Result<TripDto>>
{
    public async Task<Result<TripDto>> Handle(PostponeNoDriverSearchCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var passengerId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (trip.PassengerId != passengerId)
        {
            return TripErrors.NotOwnedByPassenger;
        }

        var result = trip.PostponeNoDriverSearch();
        if (result.IsError)
        {
            return result.Errors;
        }

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // An admin accepted the trip between our read and save — no longer
            // in a state to postpone.
            return TripErrors.InvalidStatus(trip.Status);
        }

        return await TripDtoBuilder.BuildAsync(context, trip, timeProvider.GetUtcNow(), ct);
    }
}
