using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Vehicles.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Vehicles;

namespace Taxi.Application.Features.Vehicles.Commands.CreateVehicle;

public class CreateVehicleCommandHandler(
    IAppDbContext context) : IRequestHandler<CreateVehicleCommand, Result<VehicleDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<VehicleDto>> Handle(CreateVehicleCommand request, CancellationToken ct)
    {
        var vehicleResult = Vehicle.Create(
            Guid.NewGuid(),
            request.VehicleTypeId,
            request.DriverId,
            request.Make,
            request.Model,
            request.Year,
            request.Color,
            request.LicensePlate);

        if (vehicleResult.IsFailure)
        {
            return vehicleResult.Error;
        }

        var vehicle = vehicleResult.Value;

        _context.Vehicles.Add(vehicle);
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
