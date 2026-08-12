using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Storage;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IWelcomeEmailService welcomeEmailService,
    IFileStorage fileStorage) : IRequestHandler<UpdateUserProfileCommand, Result<UserDto>>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp"];

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

        // Email is locked for accounts created via email/Google sign-in (IsEmailVerified).
        // Phone accounts keep an unverified email and may change it freely.
        if (user.IsEmailVerified
            && request.Email is not null
            && !string.Equals(request.Email.Trim(), user.Email, StringComparison.OrdinalIgnoreCase))
        {
            return AuthErrors.EmailChangeNotAllowed;
        }

        var previousEmail = user.Email;

        string? photoUrl = user.ProfilePhotoUrl;
        string? supersededPhotoUrl = null;

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

            var extension = request.PhotoContentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg",
            };
            supersededPhotoUrl = user.ProfilePhotoUrl;
            photoUrl = await fileStorage.SaveAsync(
                request.PhotoStream, StoragePaths.ProfilePhoto(userId, extension), ct);
        }

        // 2. Update Profile
        var nameToUpdate = request.Name ?? user.Name;
        var updateResult = user.UpdateProfile(nameToUpdate, photoUrl, request.Email);

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        // 3. Apply an explicit address operation so unrelated profile updates
        // never accidentally clear the stored address.
        Result<Success>? addressResult = request.HomeAddressOperation switch
        {
            HomeAddressUpdateMode.Set => user.UpdateHomeAddress(
                request.HomeAddressLabel,
                request.HomeAddressLatitude,
                request.HomeAddressLongitude),
            HomeAddressUpdateMode.Clear => user.UpdateHomeAddress(null, null, null),
            _ => null,
        };

        if (addressResult?.IsError == true)
        {
            return addressResult!.Errors;
        }

        await context.SaveChangesAsync(ct);

        // Profile photo names are random now, so a new upload no longer overwrites the old
        // file in place — remove it, otherwise every previous photo stays on disk and
        // fetchable by anyone who kept the URL.
        if (!string.IsNullOrWhiteSpace(supersededPhotoUrl)
            && !string.Equals(supersededPhotoUrl, photoUrl, StringComparison.OrdinalIgnoreCase))
        {
            await fileStorage.DeleteAsync(supersededPhotoUrl, ct);
        }

        // A phone user adding/changing an email gets the welcome mail on every change to a
        // new value. Locked (email/Google) accounts can't reach here, so this only fires for
        // phone accounts. Best-effort — never blocks the profile save.
        if (!string.IsNullOrWhiteSpace(user.Email)
            && !string.Equals(user.Email, previousEmail, StringComparison.OrdinalIgnoreCase))
        {
            await welcomeEmailService.SendWelcomeEmailAsync(user.Email, user.Name, user.PreferredLanguage, ct);
        }

        return new UserDto
        {
            Id = user.Id,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            Email = user.Email,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            Name = user.Name,
            IsPhoneVerified = user.IsPhoneVerified,
            IsEmailVerified = user.IsEmailVerified,
            HomeAddressLabel = user.HomeAddress?.Label,
            HomeAddressLatitude = user.HomeAddress?.Latitude,
            HomeAddressLongitude = user.HomeAddress?.Longitude,
        };
    }
}

