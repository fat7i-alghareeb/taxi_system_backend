using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Drivers.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Queries.GetDrivers;

public class GetDriversQueryHandler(
    ILogger<GetDriversQueryHandler> logger,
    IAppDbContext context,
    ILanguageContext languageContext) : IRequestHandler<GetDriversQuery, Result<List<DriverDto>>>
{
    private readonly ILogger<GetDriversQueryHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly ILanguageContext _languageContext = languageContext;

    public async Task<Result<List<DriverDto>>> Handle(GetDriversQuery request, CancellationToken ct)
    {
        var lang = _languageContext.Language;

        _logger.LogInformation(
            "[Projection] {QueryName} — Language='{Language}'.",
            nameof(GetDriversQuery), lang);

        var dtos = await _context.Drivers
            .AsNoTracking()
            .Join(_context.DomainUsers, d => d.UserId, u => u.Id, (d, u) => new { d, u })
            .Select(x => new DriverDto(
                x.d.Id,
                x.d.UserId,
                x.u.Name,
                x.d.LicenseNumber,
                x.d.Status.ToString(),
                x.d.ApprovalStatus.ToString(),
                x.d.VehicleTypeId))
            .ToListAsync(ct);

        return dtos;
    }
}