using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Auth.Services;

internal sealed class AuthSessionFactory(
    IIdentityService identityService,
    ITokenProvider tokenProvider,
    IAppDbContext dbContext) : IAuthSessionFactory
{
    public async Task<Result<AuthResponse>> CreateAsync(
        User domainUser,
        bool isNewAccount = false,
        bool accountAlreadyExists = false,
        CancellationToken ct = default)
    {
        var identityId = domainUser.Id.ToString();

        var appUserResult = await identityService.GetUserByIdAsync(identityId);
        if (appUserResult.IsError)
        {
            return appUserResult.Errors;
        }

        var tokenResult = await tokenProvider.GenerateJwtTokenAsync(appUserResult.Value, ct);
        if (tokenResult.IsError)
        {
            return tokenResult.Errors;
        }

        // Placeholder names (e.g. "Passenger +31…") are surfaced as null so the client
        // knows to run the profile/name step.
        var resolvedName = domainUser.Name;
        var isPlaceholder = resolvedName.StartsWith("Passenger ", StringComparison.Ordinal);

        Guid? driverId = null;
        string? approvalStatus = null;
        Guid? vehicleTypeId = null;

        if (domainUser.Role == UserRole.Driver)
        {
            var driver = await dbContext.Drivers.FirstOrDefaultAsync(d => d.UserId == domainUser.Id, ct);
            if (driver != null)
            {
                driverId = driver.Id;
                approvalStatus = driver.ApprovalStatus.ToString();
                vehicleTypeId = driver.VehicleTypeId;
            }
        }

        var userDto = new UserDto
        {
            Id = domainUser.Id,
            Phone = domainUser.Phone,
            Role = domainUser.Role.ToString(),
            Email = domainUser.Email,
            ProfilePhotoUrl = domainUser.ProfilePhotoUrl,
            Name = isPlaceholder ? null : resolvedName,
            IsPhoneVerified = domainUser.IsPhoneVerified,
            IsEmailVerified = domainUser.IsEmailVerified,
            DriverId = driverId,
            ApprovalStatus = approvalStatus,
            VehicleTypeId = vehicleTypeId,
            RequiresPasswordReset = tokenResult.Value.RequiresPasswordReset,
            HomeAddressLabel = domainUser.HomeAddress?.Label,
            HomeAddressLatitude = domainUser.HomeAddress?.Latitude,
            HomeAddressLongitude = domainUser.HomeAddress?.Longitude,
        };

        return new AuthResponse(
            tokenResult.Value.AccessToken,
            tokenResult.Value.RefreshToken,
            userDto,
            isNewAccount,
            accountAlreadyExists);
    }
}
