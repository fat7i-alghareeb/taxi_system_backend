using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetPassengerTripCount;

public class GetPassengerTripCountQueryHandler(
    IAppDbContext context,
    IUser currentUser) : IRequestHandler<GetPassengerTripCountQuery, Result<int>>
{
    public async Task<Result<int>> Handle(GetPassengerTripCountQuery request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var passengerId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var count = await context.Trips
            .CountAsync(t => t.PassengerId == passengerId, ct);

        return count;
    }
}

