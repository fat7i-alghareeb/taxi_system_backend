using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Contracts.Responses.Vehicles;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Commands.UpdateVehicleType;

public class UpdateVehicleTypeCommandHandler(
    IAppDbContext context,
    ILanguageContext languageContext) : IRequestHandler<UpdateVehicleTypeCommand, Result<VehicleTypeDto>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILanguageContext _languageContext = languageContext;

    public async Task<Result<VehicleTypeDto>> Handle(UpdateVehicleTypeCommand request, CancellationToken ct)
    {
        var vehicleType = await _context.VehicleTypes
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct);

        if (vehicleType is null)
        {
            return Error.NotFound(
                code: LocalizationKeys.Vehicle.NotFound,
                description: $"Vehicle type with ID '{request.Id}' was not found.");
        }

        vehicleType.UpdatePricing(request.RatePerKm, request.RatePerMin, request.MinFare);

        if (request.IsActive)
        {
            vehicleType.Activate();
        }
        else
        {
            vehicleType.Deactivate();
        }

        await _context.SaveChangesAsync(ct);

        return new VehicleTypeDto(
            vehicleType.Id,
            vehicleType.Code,
            vehicleType.Name.GetTranslation(_languageContext.Language),
            vehicleType.PassengerCapacity,
            vehicleType.RatePerKm,
            vehicleType.RatePerMin,
            vehicleType.MinimumFare,
            vehicleType.CurrencyCode);
    }
}

