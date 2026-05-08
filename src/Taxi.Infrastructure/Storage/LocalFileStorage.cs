using Microsoft.AspNetCore.Hosting;
using Taxi.Application.Common.Interfaces;

namespace Taxi.Infrastructure.Storage;

public class LocalFileStorage(IWebHostEnvironment environment) : IFileStorage
{
    public async Task<string> SaveAsync(Stream stream, string relativePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(environment.WebRootPath, relativePath);
        var directory = Path.GetDirectoryName(fullPath)!;

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream, ct);

        return $"/{relativePath}";
    }
}

