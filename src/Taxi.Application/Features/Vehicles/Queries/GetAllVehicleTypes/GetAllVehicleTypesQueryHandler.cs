using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Contracts.Responses.Vehicles;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Queries.GetAllVehicleTypes;

public class GetAllVehicleTypesQueryHandler(
    IAppDbContext context,
    ILanguageContext languageContext)
    : IRequestHandler<GetAllVehicleTypesQuery, Result<List<VehicleTypeDto>>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILanguageContext _languageContext = languageContext;

    public async Task<Result<List<VehicleTypeDto>>> Handle(GetAllVehicleTypesQuery request, CancellationToken cancellationToken)
    {
        var lang = _languageContext.Language;

        var vehicleTypes = await _context.VehicleTypes
            .AsNoTracking()
            .OrderBy(t => t.SortOrder)
            .Select(t => new VehicleTypeDto(
                t.Id,
                t.Code,
                lang == Languages.Ar ? t.Name.Ar : (lang == Languages.Nl ? t.Name.Nl : t.Name.En),
                t.PassengerCapacity,
                t.RatePerKm,
                t.RatePerMin,
                t.MinimumFare,
                t.SortOrder,
                t.IsActive))
            .ToListAsync(cancellationToken);

        return vehicleTypes;
    }
}
