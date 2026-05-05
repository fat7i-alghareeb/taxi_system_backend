using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Vehicles.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Queries.GetVehicleById;

public class GetVehicleByIdQueryHandler(
    IAppDbContext context) : IRequestHandler<GetVehicleByIdQuery, Result<VehicleDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<VehicleDto>> Handle(GetVehicleByIdQuery request, CancellationToken ct)
    {
        var vehicle = await _context.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.Id, ct);

        if (vehicle is null)
        {
            return Error.NotFound("Vehicle.NotFound", $"Vehicle with ID '{request.Id}' was not found.");
        }

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
