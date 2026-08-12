using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Uploads;

/// <summary>
/// Upload policy for driver KYC documents (passport, licence, ID scans).
///
/// The extension allowlist is a security control, not a convenience check: the stored
/// file lands under a path derived from this extension, so accepting <c>.html</c> or
/// <c>.svg</c> would let a driver plant active content in the API's own origin.
/// </summary>
public static class DocumentUploadPolicy
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".pdf",
    };

    public static Result<Success> Validate(Stream stream, string fileName)
    {
        if (stream is null || string.IsNullOrWhiteSpace(fileName))
        {
            return Error.Validation("Upload.DocumentInvalid", "A valid document file is required.");
        }

        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return Error.Validation("Upload.DocumentTypeInvalid", "Only JPEG, PNG, WebP, and PDF documents are supported.");
        }

        if (stream.CanSeek && stream.Length > MaxFileSizeBytes)
        {
            return Error.Validation("Upload.DocumentTooLarge", "Documents must be 10 MB or smaller.");
        }

        return Result.Success;
    }

    /// <summary>
    /// Normalizes a client-supplied file name to an allowlisted extension. Never echo the
    /// raw client extension into a storage path — <see cref="Validate"/> must have passed first.
    /// </summary>
    public static string NormalizeExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return AllowedExtensions.Contains(extension) ? extension.ToLowerInvariant() : ".bin";
    }
}
