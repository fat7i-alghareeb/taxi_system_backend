using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Users.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler(
    IAppDbContext context,
    IIdentityService identityService,
    IUser currentUser) : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var requiresPasswordReset = await identityService.RequiresPasswordResetAsync(userId.ToString());

        var adminProfile = await context.AdminProfiles.FirstOrDefaultAsync(a => a.Id == userId, ct);
        if (adminProfile is not null)
        {
            return new UserDto
            {
                Id = adminProfile.Id,
                Phone = adminProfile.Phone1 ?? string.Empty,
                Role = UserRole.Admin.ToString(),
                Email = adminProfile.Email,
                Name = adminProfile.Name,
                RequiresPasswordReset = requiresPasswordReset,
            };
        }

        var user = await context.DomainUsers
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return Error.NotFound(LocalizationKeys.User.NotFound, "User not found.");
        }

        var resolvedName = user.Name;
        var isPlaceholder = resolvedName.StartsWith("Passenger ");

        Guid? driverId = null;
        string? approvalStatus = null;

        if (user.Role == UserRole.Driver)
        {
            var driver = await context.Drivers.FirstOrDefaultAsync(d => d.UserId == user.Id, ct);
            if (driver != null)
            {
                driverId = driver.Id;
                approvalStatus = driver.ApprovalStatus.ToString();
            }
        }

        return new UserDto
        {
            Id = user.Id,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            Email = user.Email,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            Name = isPlaceholder ? null : resolvedName,
            DriverId = driverId,
            ApprovalStatus = approvalStatus,
            RequiresPasswordReset = requiresPasswordReset,
            HomeAddressLabel = user.HomeAddress?.Label,
            HomeAddressLatitude = user.HomeAddress?.Latitude,
            HomeAddressLongitude = user.HomeAddress?.Longitude,
        };
    }
}

