using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Uploads;

public static class AudioUploadPolicy
{
    public const long MaxFileSizeBytes = 25 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".m4a",
        ".aac",
        ".mp3",
        ".wav",
    };

    public static Result<Success> Validate(Stream stream, string fileName)
    {
        if (stream is null || string.IsNullOrWhiteSpace(fileName))
        {
            return Error.Validation("Upload.AudioInvalid", "A valid audio file is required.");
        }

        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return Error.Validation("Upload.AudioTypeInvalid", "Only M4A, AAC, MP3, and WAV audio is supported.");
        }

        if (stream.CanSeek && stream.Length > MaxFileSizeBytes)
        {
            return Error.Validation("Upload.AudioTooLarge", "Recordings must be 25 MB or smaller.");
        }

        return Result.Success;
    }
}
