using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Contracts.Responses.Vehicles;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Queries.GetVehicleCatalog;

public class GetVehicleCatalogQueryHandler(
    ILogger<GetVehicleCatalogQueryHandler> logger,
    IAppDbContext context,
    ILanguageContext languageContext) : IRequestHandler<GetVehicleCatalogQuery, Result<List<VehicleTypeDto>>>
{
    private readonly ILogger<GetVehicleCatalogQueryHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly ILanguageContext _languageContext = languageContext;

    public async Task<Result<List<VehicleTypeDto>>> Handle(GetVehicleCatalogQuery request, CancellationToken cancellationToken)
    {
        var lang = _languageContext.Language;

        _logger.LogInformation(
            "[Projection] {QueryName} — Language='{Language}'. " +
            "PostgreSQL will extract Name->>'{JsonKey}' from the JSONB column. Full trilingual load: DISABLED.",
            nameof(GetVehicleCatalogQuery), lang, lang == Languages.Ar ? "Ar" : (lang == Languages.Nl ? "Nl" : "En"));

        var vehicleTypes = await _context.VehicleTypes
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .Select(t => new VehicleTypeDto(
                t.Id,
                t.Code,
                lang == Languages.Ar ? t.Name.Ar : (lang == Languages.Nl ? t.Name.Nl : t.Name.En),
                t.PassengerCapacity,
                t.RatePerKm,
                t.RatePerMin,
                t.MinimumFare,
                t.SortOrder))
            .ToListAsync(cancellationToken);

        return vehicleTypes;
    }
}

