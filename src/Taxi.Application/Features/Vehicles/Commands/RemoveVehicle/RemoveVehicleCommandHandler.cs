using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Vehicles;

namespace Taxi.Application.Features.Vehicles.Commands.RemoveVehicle;

public class RemoveVehicleCommandHandler(
    IAppDbContext context) : IRequestHandler<RemoveVehicleCommand, Result<Deleted>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Deleted>> Handle(RemoveVehicleCommand request, CancellationToken ct)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.Id, ct);

        if (vehicle is null)
        {
            return VehicleErrors.NotFound;
        }

        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync(ct);

        return Result.Deleted;
    }
}

