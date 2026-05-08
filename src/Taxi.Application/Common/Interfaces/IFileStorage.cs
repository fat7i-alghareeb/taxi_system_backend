namespace Taxi.Application.Common.Interfaces;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream stream, string relativePath, CancellationToken ct = default);
}

