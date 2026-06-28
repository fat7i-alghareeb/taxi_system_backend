using Microsoft.AspNetCore.Hosting;

namespace Taxi.Infrastructure.Storage;

/// <summary>
/// Resolves the physical web root that backs uploaded-file storage. Shared by
/// <see cref="LocalFileStorage"/> and <c>StorageInitializer</c> so both agree on
/// the same directory. Falls back to <c>ContentRoot/wwwroot</c> when
/// <see cref="IWebHostEnvironment.WebRootPath"/> is null (e.g. the folder did not
/// exist when the host was built).
/// </summary>
internal static class StorageRoot
{
    public static string Resolve(IWebHostEnvironment environment) =>
        environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
}
