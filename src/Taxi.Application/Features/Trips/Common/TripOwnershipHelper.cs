using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Common;

/// <summary>
/// Builds the <see cref="TripErrors.NotAcceptedByCurrentAdmin"/> error enriched with the
/// name of the admin who actually accepted the trip, so the dashboard can tell the current
/// admin who the trip belongs to. Reused by every trip-action handler that guards on
/// <c>trip.AcceptedByAdminId</c>.
/// </summary>
public static class TripOwnershipHelper
{
    public static async Task<Error> NotOwnedByCurrentAdminAsync(
        IAppDbContext context, Guid? acceptedByAdminId, CancellationToken ct)
    {
        var ownerName = acceptedByAdminId.HasValue
            ? await context.AdminProfiles.AsNoTracking()
                .Where(a => a.Id == acceptedByAdminId.Value)
                .Select(a => a.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        // Fallback keeps the sentence grammatical in the rare case the trip has no
        // accepting admin yet (e.g. an action attempted while still AwaitingAdminAcceptance),
        // matching the previous generic wording.
        return TripErrors.NotAcceptedByCurrentAdmin(
            string.IsNullOrWhiteSpace(ownerName) ? "another admin" : ownerName);
    }
}
