using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.UploadProfilePhoto;

public class UploadProfilePhotoCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IFileStorage fileStorage) : IRequestHandler<UploadProfilePhotoCommand, Result<string>>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png"];

    public async Task<Result<string>> Handle(UploadProfilePhotoCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        if (!AllowedContentTypes.Contains(request.ContentType))
        {
            return Error.Validation(LocalizationKeys.User.ProfilePhotoInvalid, "Photo must be a JPEG or PNG image.");
        }

        if (request.FileStream.Length > MaxFileSizeBytes)
        {
            return Error.Validation(LocalizationKeys.User.ProfilePhotoInvalid, "Photo must be under 5MB.");
        }

        var user = await context.DomainUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return Error.NotFound(LocalizationKeys.User.NotFound, "User not found.");
        }

        var extension = request.ContentType == "image/png" ? ".png" : ".jpg";
        var storedUrl = await fileStorage.SaveAsync(request.FileStream, $"photos/{userId}{extension}", ct);

        user.UpdateProfile(user.Name, storedUrl);
        await context.SaveChangesAsync(ct);

        return storedUrl;
    }
}

