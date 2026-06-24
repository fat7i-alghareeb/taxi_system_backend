using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Queries.GetAllDriversWithStatus;

public class GetAllDriversWithStatusQueryHandler(IAppDbContext context)
    : IRequestHandler<GetAllDriversWithStatusQuery, Result<List<DriverWithStatusDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<DriverWithStatusDto>>> Handle(GetAllDriversWithStatusQuery request, CancellationToken ct)
    {
        var drivers = await _context.Drivers
            .AsNoTracking()
            .Where(d => d.DeletedAtUtc == null)
            .ToListAsync(ct);

        var list = new List<DriverWithStatusDto>();

        foreach (var driver in drivers)
        {
            var user = await _context.DomainUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == driver.UserId, ct);
            if (user == null)
            {
                continue;
            }

            string? vehicleTypeName = null;
            if (driver.VehicleTypeId.HasValue)
            {
                var vehicleType = await _context.VehicleTypes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == driver.VehicleTypeId.Value, ct);
                vehicleTypeName = vehicleType?.Name.En;
            }

            list.Add(new DriverWithStatusDto(
                driver.Id,
                user.Name,
                user.Phone,
                driver.Status.ToString(),
                driver.ApprovalStatus.ToString(),
                driver.CurrentLat.HasValue ? (double)driver.CurrentLat.Value : null,
                driver.CurrentLng.HasValue ? (double)driver.CurrentLng.Value : null,
                driver.LocationUpdatedAt,
                driver.VehicleTypeId,
                vehicleTypeName));
        }

        return list;
    }
}
