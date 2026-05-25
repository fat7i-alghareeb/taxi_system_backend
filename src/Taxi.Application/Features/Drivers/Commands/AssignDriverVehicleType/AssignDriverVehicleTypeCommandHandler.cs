using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Commands.AssignDriverVehicleType;

public class AssignDriverVehicleTypeCommandHandler(IAppDbContext context)
    : IRequestHandler<AssignDriverVehicleTypeCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(AssignDriverVehicleTypeCommand request, CancellationToken ct)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == request.DriverId, ct);
        if (driver is null)
        {
            return Error.NotFound("Driver.NotFound", $"Driver with ID {request.DriverId} was not found.");
        }

        var vehicleTypeExists = await _context.VehicleTypes.AnyAsync(t => t.Id == request.VehicleTypeId, ct);
        if (!vehicleTypeExists)
        {
            return Error.NotFound("VehicleType.NotFound", $"Vehicle type with ID {request.VehicleTypeId} was not found.");
        }

        driver.SetVehicleType(request.VehicleTypeId);
        await _context.SaveChangesAsync(ct);

        return Result.Success;
    }
}