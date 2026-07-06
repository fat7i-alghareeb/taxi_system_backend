using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Auth.Services;

/// <summary>
/// Assigns an FCM device token to the account signing in now, clearing it from any other
/// account that previously registered it on the same device so stale users stop receiving
/// push notifications meant for the current user.
/// </summary>
internal static class FcmTokenSync
{
    public static async Task ApplyAsync(IAppDbContext dbContext, User user, string? fcmToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fcmToken))
        {
            return;
        }

        var previousOwners = await dbContext.DomainUsers
            .IgnoreQueryFilters()
            .Where(u => u.FcmToken == fcmToken && u.Id != user.Id)
            .ToListAsync(ct);

        foreach (var previousOwner in previousOwners)
        {
            previousOwner.UpdateFcmToken(null);
        }

        user.UpdateFcmToken(fcmToken);
    }
}
