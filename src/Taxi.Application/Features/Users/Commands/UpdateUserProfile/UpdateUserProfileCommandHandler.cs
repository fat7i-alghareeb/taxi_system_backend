using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IFileStorage fileStorage,
    ILanguageContext languageContext) : IRequestHandler<UpdateUserProfileCommand, Result<UserDto>>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png"];

    public async Task<Result<UserDto>> Handle(UpdateUserProfileCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var user = await context.DomainUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return Error.NotFound(LocalizationKeys.User.NotFound, "User not found.");
        }

        string? photoUrl = user.ProfilePhotoUrl;

        // 1. Handle Photo Upload
        if (request.PhotoStream != null)
        {
            if (!AllowedContentTypes.Contains(request.PhotoContentType))
            {
                return Error.Validation(LocalizationKeys.User.ProfilePhotoInvalid, "Photo must be a JPEG or PNG image.");
            }

            if (request.PhotoStream.Length > MaxFileSizeBytes)
            {
                return Error.Validation(LocalizationKeys.User.ProfilePhotoInvalid, "Photo must be under 5MB.");
            }

            var extension = request.PhotoContentType == "image/png" ? ".png" : ".jpg";
            photoUrl = await fileStorage.SaveAsync(request.PhotoStream, $"photos/{userId}{extension}", ct);
        }

        // 2. Update Profile
        var nameToUpdate = request.Name ?? user.Name;
        var updateResult = user.UpdateProfile(nameToUpdate, photoUrl);

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        return new UserDto
        {
            Id = user.Id,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            Email = user.Email,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            Name = user.Name,
        };
    }
}

