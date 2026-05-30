using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Drivers;
using Taxi.Domain.Trips;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Trips.Commands.AdminTakeTrip;

public class AdminTakeTripCommandHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<AdminTakeTripCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;
    private readonly IUser _currentUser = currentUser;

    public async Task<Result<Success>> Handle(AdminTakeTripCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminUserId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.Unauthorized, "Unauthorized user.");
        }

        var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        // Find-or-create the admin's backing Driver record.
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == adminUserId, ct);
        if (driver is null)
        {
            // Drivers.UserId has a required FK to DomainUsers, but admins are
            // created as identity users + an AdminProfile only. Ensure a backing
            // DomainUsers row exists first, otherwise the insert violates the FK.
            var ensureUserResult = await EnsureAdminDomainUserAsync(adminUserId, ct);
            if (ensureUserResult.IsFailure)
            {
                return ensureUserResult.Error;
            }

            var createResult = Driver.Create(
                id: Guid.NewGuid(),
                userId: adminUserId,
                licenseNumber: $"ADMIN-{adminUserId.ToString("N")[..8].ToUpper()}");
            if (createResult.IsFailure)
            {
                return createResult.Error;
            }

            driver = createResult.Value;

            // Admin-as-driver is implicitly approved and inherits the trip's vehicle type.
            driver.Approve();
            driver.SetVehicleType(trip.VehicleTypeId);

            _context.Drivers.Add(driver);
        }
        else if (driver.VehicleTypeId != trip.VehicleTypeId)
        {
            // Re-target the admin's driver record at the trip's vehicle type.
            driver.SetVehicleType(trip.VehicleTypeId);
        }

        // Bypass the standard vehicle-type-match gate by assigning directly.
        var assignResult = trip.AssignDriver(driver.Id);
        if (assignResult.IsFailure)
        {
            return assignResult.Error;
        }

        driver.SetStatus(DriverStatus.OnTrip);

        await _context.SaveChangesAsync(ct);
        return Result.Success;
    }

    private async Task<Result<Success>> EnsureAdminDomainUserAsync(
        Guid adminUserId,
        CancellationToken ct)
    {
        var exists = await _context.DomainUsers.AnyAsync(u => u.Id == adminUserId, ct);
        if (exists)
        {
            return Result.Success;
        }

        var adminProfile = await _context.AdminProfiles
            .FirstOrDefaultAsync(a => a.Id == adminUserId, ct);

        var name = string.IsNullOrWhiteSpace(adminProfile?.Name)
            ? "Admin"
            : adminProfile!.Name;

        // User.Phone is unique and required; admins authenticate by username, so
        // use a synthetic numeric phone derived from the admin id (never collides
        // with real customer/driver numbers).
        var phone = BuildAdminPlaceholderPhone(adminUserId);

        var userResult = User.CreateAdmin(adminUserId, name, phone, adminProfile?.Email);
        if (userResult.IsFailure)
        {
            return userResult.Error;
        }

        _context.DomainUsers.Add(userResult.Value);
        return Result.Success;
    }

    private static string BuildAdminPlaceholderPhone(Guid adminUserId)
    {
        // Clear the sign bit (avoids Math.Abs overflow on long.MinValue).
        var value = BitConverter.ToInt64(adminUserId.ToByteArray(), 0) & long.MaxValue;

        // 13-digit number with no leading zero, prefixed with '+' (matches the
        // ^\+?\d{7,15}$ phone rule).
        var digits = ((value % 9_000_000_000_000L) + 1_000_000_000_000L).ToString();
        return $"+{digits}";
    }
}
