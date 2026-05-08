using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Commands.RemoveVehicleType;

public class RemoveVehicleTypeCommandHandler(
    IAppDbContext context) : IRequestHandler<RemoveVehicleTypeCommand, Result<Deleted>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Deleted>> Handle(RemoveVehicleTypeCommand request, CancellationToken ct)
    {
        var vehicleType = await _context.VehicleTypes
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct);

        if (vehicleType is null)
        {
            return Error.NotFound(
                code: LocalizationKeys.Vehicle.NotFound,
                description: $"Vehicle type with ID '{request.Id}' was not found.");
        }

        _context.VehicleTypes.Remove(vehicleType);
        await _context.SaveChangesAsync(ct);

        return Result.Deleted;
    }
}

