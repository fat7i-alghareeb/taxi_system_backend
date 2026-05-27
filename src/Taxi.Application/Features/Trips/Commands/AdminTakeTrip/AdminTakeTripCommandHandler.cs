using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Drivers;
using Taxi.Domain.Trips;

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
}
