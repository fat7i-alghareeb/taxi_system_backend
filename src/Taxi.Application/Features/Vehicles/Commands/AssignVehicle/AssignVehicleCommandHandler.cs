using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;
using Taxi.Domain.Vehicles;

namespace Taxi.Application.Features.Vehicles.Commands.AssignVehicle;

public class AssignVehicleCommandHandler(
    IAppDbContext context) : IRequestHandler<AssignVehicleCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(AssignVehicleCommand request, CancellationToken ct)
    {
        var driverUser = await _context.DomainUsers
            .FirstOrDefaultAsync(u => u.Id == request.DriverId, ct);

        if (driverUser is null)
        {
            return Error.NotFound("User.NotFound", "Driver not found.");
        }

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, ct);

        if (vehicle is null)
        {
            return Error.NotFound("Vehicle.NotFound", "Vehicle not found.");
        }

        var result = driverUser.AssignVehicle(vehicle.Id);
        if (result.IsFailure)
        {
            return result;
        }

        // Also update the Driver profile if it exists
        var driverProfile = await _context.Drivers
            .FirstOrDefaultAsync(d => d.UserId == request.DriverId, ct);

        if (driverProfile is not null)
        {
            driverProfile.SetActiveVehicle(vehicle.Id);
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success;
    }
}
