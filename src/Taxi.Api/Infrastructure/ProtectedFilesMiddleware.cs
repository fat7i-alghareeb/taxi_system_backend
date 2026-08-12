using System.Security.Claims;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Storage;

namespace Taxi.Api.Infrastructure;

/// <summary>
/// Authorization gate in front of <c>UseStaticFiles</c> for driver KYC documents.
///
/// Static-file middleware performs no authorization at all, so every uploaded artifact was
/// reachable anonymously by anyone who could construct the URL — and the document path was
/// fully deterministic (<c>documents/{driverId}_{Type}{ext}</c>), so "the URL is unguessable"
/// was not a control for the most sensitive area on the platform: passport, licence and ID
/// scans.
///
/// Scope note — why only <see cref="StoragePaths.Documents"/>:
/// the other upload areas (chat photos, recordings, compensation evidence, profile photos)
/// are fetched by the mobile apps through <c>CachedNetworkImage</c>, which bypasses the Dio
/// client and therefore sends no Authorization header. Gating those here would break every
/// avatar and in-trip photo. They are instead stored under unguessable random paths
/// (see <see cref="StoragePaths"/>), and the endpoints that disclose those URLs are
/// authorized. That is a capability-URL model, weaker than real per-object authorization:
/// anyone who obtains the URL retains access. Replacing it with short-lived signed URLs is
/// the proper follow-up, and would let this middleware cover every area uniformly.
///
/// This runs after authentication and rejects the request before the file is streamed.
/// Ordering is enforced in <c>UseCoreMiddlewares</c>: authentication → this → static files.
/// </summary>
public sealed class ProtectedFilesMiddleware(RequestDelegate next, ILogger<ProtectedFilesMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IAppDbContext db)
    {
        var path = context.Request.Path;

        if (!IsProtectedArea(path))
        {
            await next(context);
            return;
        }

        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            await RejectAsync(context, StatusCodes.Status401Unauthorized, path, "anonymous");
            return;
        }

        var isAdmin = user.IsInRole("Admin");
        if (isAdmin)
        {
            await next(context);
            return;
        }

        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            await RejectAsync(context, StatusCodes.Status401Unauthorized, path, "no-user-id");
            return;
        }

        var allowed = await OwnsDriverDocumentAsync(db, path, userId, context.RequestAborted);

        if (!allowed)
        {
            await RejectAsync(context, StatusCodes.Status403Forbidden, path, userId.ToString());
            return;
        }

        await next(context);
    }

    private static bool IsProtectedArea(PathString path) =>
        path.HasValue
        && path.StartsWithSegments($"/{StoragePaths.Documents}", StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns the n-th path segment (1-based), or null when absent.</summary>
    private static string? SegmentAt(PathString path, int index)
    {
        var segments = path.Value!.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= index ? segments[index - 1] : null;
    }

    /// <summary>
    /// Resolves the owning driver from the storage path and checks it against the caller.
    ///
    /// Two layouts are accepted:
    ///   current — <c>documents/{driverId:N}/{random:N}{ext}</c>
    ///   legacy  — <c>documents/{driverId}_{DocumentType}{ext}</c>
    ///
    /// The legacy branch exists because documents uploaded before the path change are still
    /// referenced by <c>DriverDocument.FileUrl</c> rows. Dropping it would 403 every existing
    /// driver on their own KYC screen after deploy.
    /// </summary>
    private static async Task<bool> OwnsDriverDocumentAsync(
        IAppDbContext db,
        PathString path,
        Guid userId,
        CancellationToken ct)
    {
        if (!TryResolveDriverId(path, out var driverId))
        {
            return false;
        }

        return await db.Drivers.AsNoTracking().AnyAsync(d => d.Id == driverId && d.UserId == userId, ct);
    }

    private static bool TryResolveDriverId(PathString path, out Guid driverId)
    {
        driverId = Guid.Empty;

        var second = SegmentAt(path, 2);
        if (string.IsNullOrEmpty(second))
        {
            return false;
        }

        // Current layout: the driver id is its own segment.
        if (Guid.TryParse(second, out driverId))
        {
            return true;
        }

        // Legacy layout: "{driverId}_{DocumentType}{ext}" as a single file name.
        var separator = second.IndexOf('_');
        return separator > 0 && Guid.TryParse(second[..separator], out driverId);
    }

    private Task RejectAsync(HttpContext context, int statusCode, PathString path, string subject)
    {
        logger.LogWarning(
            "[ProtectedFiles] Denied {StatusCode} path={Path} subject={Subject}",
            statusCode,
            path,
            subject);

        context.Response.StatusCode = statusCode;
        context.Response.ContentLength = 0;
        return Task.CompletedTask;
    }
}
