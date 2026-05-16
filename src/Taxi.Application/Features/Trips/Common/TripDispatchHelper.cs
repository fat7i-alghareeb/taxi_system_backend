using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Common;

/// <summary>
/// Shared driver-assignment routine used by both the legacy paymentless path
/// (when the Stripe feature flag is off) and the Stripe webhook handler (after
/// payment_intent.succeeded fires). Picks the first active driver and calls
/// <see cref="Trip.AssignDriver"/>.
/// </summary>
public static class TripDispatchHelper
{
    public static async Task<Result<Success>> AssignDefaultDriverAsync(
        Trip trip,
        IAppDbContext context,
        CancellationToken ct)
    {
        var adminDriver = await context.Drivers
            .FirstOrDefaultAsync(d => d.IsActive, ct);

        if (adminDriver is null)
        {
            return TripErrors.DriverNotFound;
        }

        var assignResult = trip.AssignDriver(adminDriver.UserId);
        if (assignResult.IsFailure)
        {
            return assignResult.Error;
        }

        return Result.Success;
    }
}
