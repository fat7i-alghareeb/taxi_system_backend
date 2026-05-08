using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Vehicles;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;
using Taxi.Domain.Users;
using Taxi.Domain.Vehicles;

namespace Taxi.Application.Features.Vehicles.Commands.CreateVehicle;

public class CreateVehicleCommandHandler(
    IAppDbContext context) : IRequestHandler<CreateVehicleCommand, Result<VehicleDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<VehicleDto>> Handle(CreateVehicleCommand request, CancellationToken ct)
    {
        var vehicleTypeExists = await _context.VehicleTypes
            .AnyAsync(vt => vt.Id == request.VehicleTypeId && vt.IsActive, ct);

        if (!vehicleTypeExists)
        {
            return TripErrors.VehicleTypeNotFound;
        }

        var driverExists = await _context.DomainUsers
            .AnyAsync(u => u.Id == request.DriverId && u.Role == UserRole.Driver, ct);

        if (!driverExists)
        {
            return VehicleErrors.DriverNotFound;
        }

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

