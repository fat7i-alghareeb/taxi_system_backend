using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Storage;

namespace Taxi.Infrastructure.Storage;

/// <summary>
/// Ensures every storage area in <see cref="StoragePaths.AllAreas"/> exists and that the
/// web root is writable, before the app serves traffic. Fails fast (throws) on a
/// misconfigured or read-only <c>wwwroot</c> so a broken volume is loud at startup
/// instead of surfacing as a silent 500 on the first user upload.
/// </summary>
public sealed class StorageInitializer(
    IWebHostEnvironment environment,
    ILogger<StorageInitializer> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var root = StorageRoot.Resolve(environment);

        try
        {
            Directory.CreateDirectory(root);

            foreach (var area in StoragePaths.AllAreas)
            {
                Directory.CreateDirectory(Path.Combine(root, area));
            }

            // Write-probe: prove the app user can actually create files here, not just
            // that the directories exist (a root-owned volume lets us read but not write).
            var probe = Path.Combine(root, $".write-probe-{Guid.NewGuid():N}");
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
        }
        catch (Exception ex)
        {
            const string message =
                "Storage is not writable at {StorageRoot}. Uploaded files (chat photos, driver "
                + "documents, recordings, etc.) cannot be saved. Check the wwwroot volume "
                + "ownership/permissions.";
            logger.LogCritical(ex, message, root);
            throw;
        }

        logger.LogInformation(
            "StorageInitializer: ensured {Count} area(s) under {StorageRoot}: {Areas}",
            StoragePaths.AllAreas.Count,
            root,
            string.Join(", ", StoragePaths.AllAreas));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
