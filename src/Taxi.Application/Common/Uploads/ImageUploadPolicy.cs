using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Uploads;

public static class ImageUploadPolicy
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
    };

    public static Result<Success> Validate(Stream stream, string fileName)
    {
        if (stream is null || string.IsNullOrWhiteSpace(fileName))
        {
            return Error.Validation("Upload.ImageInvalid", "A valid image file is required.");
        }

        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return Error.Validation("Upload.ImageTypeInvalid", "Only JPEG, PNG, and WebP images are supported.");
        }

        if (stream.CanSeek && stream.Length > MaxFileSizeBytes)
        {
            return Error.Validation("Upload.ImageTooLarge", "Images must be 10 MB or smaller.");
        }

        return Result.Success;
    }
}
