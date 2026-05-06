using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Vehicles.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Commands.UpdateVehicle;

public class UpdateVehicleCommandHandler(
    IAppDbContext context) : IRequestHandler<UpdateVehicleCommand, Result<VehicleDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<VehicleDto>> Handle(UpdateVehicleCommand request, CancellationToken ct)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.Id, ct);

        if (vehicle is null)
        {
            return Error.NotFound("Vehicle.NotFound", $"Vehicle with ID '{request.Id}' was not found.");
        }

        vehicle.UpdateDetails(request.Color, request.LicensePlate);

        if (request.IsActive)
        {
            vehicle.Activate();
        }
        else
        {
            vehicle.Deactivate();
        }

        await _context.SaveChangesAsync(ct);

        return new VehicleDto(
            vehicle.Id,
            vehicle.VehicleTypeId,
            vehicle.DriverId,
            vehicle.Make,
            vehicle.Model,
            vehicle.Year,
            vehicle.Color,
            vehicle.LicensePlate,
            vehicle.IsActive);
    }
}
