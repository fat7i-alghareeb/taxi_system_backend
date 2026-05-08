using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Contracts.Responses.Vehicles;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Queries.GetVehicleTypeById;

public class GetVehicleTypeByIdQueryHandler(
    ILogger<GetVehicleTypeByIdQueryHandler> logger,
    IAppDbContext context,
    ILanguageContext languageContext)
    : IRequestHandler<GetVehicleTypeByIdQuery, Result<VehicleTypeDto>>
{
    private readonly ILogger<GetVehicleTypeByIdQueryHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly ILanguageContext _languageContext = languageContext;

    public async Task<Result<VehicleTypeDto>> Handle(GetVehicleTypeByIdQuery query, CancellationToken ct)
    {
        var lang = _languageContext.Language;

        _logger.LogInformation(
            "[Projection] {QueryName} — Language='{Language}'. " +
            "PostgreSQL will extract Name->>'{JsonKey}' from the JSONB column. Full trilingual load: DISABLED.",
            nameof(GetVehicleTypeByIdQuery), lang, lang == Languages.Ar ? "Ar" : (lang == Languages.Nl ? "Nl" : "En"));

        var vehicleType = await _context.VehicleTypes
            .AsNoTracking()
            .Where(t => t.Id == query.Id)
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
            _logger.LogWarning("Vehicle type with ID {Id} was not found", query.Id);

            return Error.NotFound(
                code: LocalizationKeys.Vehicle.NotFound,
                description: $"Vehicle type with ID '{query.Id}' was not found");
        }

        return vehicleType;
    }
}

