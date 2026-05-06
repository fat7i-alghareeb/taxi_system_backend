using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Vehicles.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Queries.GetVehicleTypeByCode;

public class GetVehicleTypeByCodeQueryHandler(
    ILogger<GetVehicleTypeByCodeQueryHandler> logger,
    IAppDbContext context,
    ILanguageContext languageContext)
    : IRequestHandler<GetVehicleTypeByCodeQuery, Result<VehicleTypeDto>>
{
    private readonly ILogger<GetVehicleTypeByCodeQueryHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly ILanguageContext _languageContext = languageContext;

    public async Task<Result<VehicleTypeDto>> Handle(GetVehicleTypeByCodeQuery query, CancellationToken ct)
    {
        var lang = _languageContext.Language;

        _logger.LogInformation(
            "[Projection] {QueryName} — Language='{Language}'. " +
            "PostgreSQL will extract Name->>'{JsonKey}' from the JSONB column. Full trilingual load: DISABLED.",
            nameof(GetVehicleTypeByCodeQuery), lang, lang == Languages.Ar ? "Ar" : (lang == Languages.Nl ? "Nl" : "En"));

        var vehicleType = await _context.VehicleTypes
            .AsNoTracking()
            .Where(t => t.Code == query.Code)
            .Select(t => new VehicleTypeDto(
                t.Id,
                t.Code,
                lang == Languages.Ar ? t.Name.Ar : (lang == Languages.Nl ? t.Name.Nl : t.Name.En),
                t.PassengerCapacity,
                t.RatePerKm,
                t.RatePerMin,
                t.MinimumFare,
                t.CurrencyCode))
            .FirstOrDefaultAsync(ct);

        if (vehicleType is null)
        {
            _logger.LogWarning("Vehicle type with code {Code} was not found", query.Code);

            return Error.NotFound(
                code: LocalizationKeys.Vehicle.NotFound,
                description: $"Vehicle type with code '{query.Code}' was not found");
        }

        return vehicleType;
    }
}
