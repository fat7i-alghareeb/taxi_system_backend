using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Vehicles.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Queries.GetVehicles;

public class GetVehiclesQueryHandler(
    IAppDbContext context) : IRequestHandler<GetVehiclesQuery, Result<List<VehicleDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<VehicleDto>>> Handle(GetVehiclesQuery request, CancellationToken ct)
    {
        return await _context.Vehicles
            .AsNoTracking()
            .Select(v => new VehicleDto(
                v.Id,
                v.VehicleTypeId,
                v.DriverId,
                v.Make,
                v.Model,
                v.Year,
                v.Color,
                v.LicensePlate,
                v.IsActive))
            .ToListAsync(ct);
    }
}
