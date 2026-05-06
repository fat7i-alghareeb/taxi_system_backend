using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Drivers.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Queries.GetDrivers;

public class GetDriversQueryHandler(
    IAppDbContext context,
    ILanguageContext languageContext) : IRequestHandler<GetDriversQuery, Result<List<DriverDto>>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILanguageContext _languageContext = languageContext;

    public async Task<Result<List<DriverDto>>> Handle(GetDriversQuery request, CancellationToken ct)
    {
        var drivers = await _context.Drivers
            .AsNoTracking()
            .ToListAsync(ct);

        var userIds = drivers.Select(d => d.UserId).Distinct().ToList();
        var users = await _context.DomainUsers
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(ct);

        var userMap = users.ToDictionary(u => u.Id);

        var dtos = drivers.Select(driver =>
        {
            userMap.TryGetValue(driver.UserId, out var user);
            return new DriverDto(
                driver.Id,
                driver.UserId,
                user?.Name?.GetTranslation(_languageContext.Language),
                driver.LicenseNumber,
                driver.Status.ToString(),
                driver.ActiveVehicleId);
        }).ToList();

        return dtos;
    }
}
