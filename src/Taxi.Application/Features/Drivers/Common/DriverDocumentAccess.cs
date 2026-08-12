using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Drivers;

namespace Taxi.Application.Features.Drivers.Queries.GetDriverDocuments;

/// <summary>
/// Ownership gate for the <c>/drivers/{id}/documents</c> routes.
///
/// The endpoints are role-gated <c>Admin,Driver</c>, which only says a *driver* may call
/// them — not *which* driver's documents may be touched. Without this check any approved
/// driver could read another driver's passport, or overwrite it (upload soft-deletes the
/// existing document of the same type). Admins are unrestricted by design.
/// </summary>
internal static class DriverDocumentAccess
{
    public static async Task<Result<Success>> AuthorizeAsync(
        IAppDbContext context,
        IUser currentUser,
        Guid targetDriverId,
        CancellationToken ct)
    {
        if (currentUser.IsAdmin)
        {
            return Result.Success;
        }

        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var ownsDriver = await context.Drivers
            .AsNoTracking()
            .AnyAsync(d => d.Id == targetDriverId && d.UserId == userId, ct);

        return ownsDriver ? Result.Success : DriverErrors.DocumentsNotOwned;
    }
}
