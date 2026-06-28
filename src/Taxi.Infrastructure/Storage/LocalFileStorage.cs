using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Taxi.Application.Common.Interfaces;
using Taxi.Infrastructure.Settings;

namespace Taxi.Infrastructure.Storage;

public class LocalFileStorage(
    IWebHostEnvironment environment,
    IOptions<AppSettings> appSettings) : IFileStorage
{
    private readonly string _apiBaseUrl = appSettings.Value.ApiBaseUrl.TrimEnd('/');

    public async Task<string> SaveAsync(Stream stream, string relativePath, CancellationToken ct = default)
    {
        var rootPath = StorageRoot.Resolve(environment);
        var fullPath = Path.Combine(rootPath, relativePath);
        var directory = Path.GetDirectoryName(fullPath)!;

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream, ct);

        var webPath = relativePath.Replace('\\', '/');
        var relativeUrl = webPath.StartsWith('/') ? webPath : $"/{webPath}";

        // Return absolute URL when base URL is configured, otherwise relative.
        return string.IsNullOrEmpty(_apiBaseUrl) ? relativeUrl : $"{_apiBaseUrl}{relativeUrl}";
    }

    public Task DeleteAsync(string relativePathOrUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(relativePathOrUrl))
        {
            return Task.CompletedTask;
        }

        // Accept either the absolute URL returned by SaveAsync or the raw relative path.
        var relativePath = relativePathOrUrl;
        if (!string.IsNullOrEmpty(_apiBaseUrl) && relativePath.StartsWith(_apiBaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = relativePath[_apiBaseUrl.Length..];
        }

        relativePath = relativePath.TrimStart('/', '\\');

        var rootPath = StorageRoot.Resolve(environment);
        var fullPath = Path.Combine(rootPath, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}

