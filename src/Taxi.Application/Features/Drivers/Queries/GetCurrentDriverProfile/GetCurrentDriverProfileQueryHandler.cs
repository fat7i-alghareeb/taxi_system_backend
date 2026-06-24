using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Drivers.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Queries.GetCurrentDriverProfile;

public class GetCurrentDriverProfileQueryHandler(
    IAppDbContext context,
    IUser currentUser) : IRequestHandler<GetCurrentDriverProfileQuery, Result<DriverCurrentProfileDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<DriverCurrentProfileDto>> Handle(
        GetCurrentDriverProfileQuery request,
        CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var user = await _context.DomainUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Error.NotFound(LocalizationKeys.User.NotFound, "User not found.");
        }

        var driver = await _context.Drivers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId, ct);
        if (driver is null)
        {
            return Error.NotFound(LocalizationKeys.Driver.NotFound, "Driver profile not found.");
        }

        string? vehicleTypeName = null;
        if (driver.VehicleTypeId.HasValue)
        {
            var vehicleType = await _context.VehicleTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == driver.VehicleTypeId.Value, ct);
            vehicleTypeName = vehicleType?.Name.En;
        }

        return new DriverCurrentProfileDto(
            user.Id,
            driver.Id,
            user.Name,
            user.Email,
            user.Phone,
            user.ProfilePhotoUrl,
            driver.LicenseNumber,
            driver.ApprovalStatus.ToString(),
            driver.VehicleTypeId,
            vehicleTypeName);
    }
}
