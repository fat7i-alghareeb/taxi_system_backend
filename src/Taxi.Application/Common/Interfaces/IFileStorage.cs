namespace Taxi.Application.Common.Interfaces;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream stream, string relativePath, CancellationToken ct = default);

    /// <summary>
    /// Deletes a previously saved file. Accepts either the relative path used with
    /// <see cref="SaveAsync"/> or the absolute URL it returned. Missing files are
    /// ignored (no exception).
    /// </summary>
    Task DeleteAsync(string relativePathOrUrl, CancellationToken ct = default);
}
