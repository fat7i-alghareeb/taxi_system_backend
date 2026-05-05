using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Vehicles.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Vehicles;

namespace Taxi.Application.Features.Vehicles.Commands.CreateVehicleType;

public class CreateVehicleTypeCommandHandler(
    IAppDbContext context,
    ILanguageContext languageContext) : IRequestHandler<CreateVehicleTypeCommand, Result<VehicleTypeDto>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILanguageContext _languageContext = languageContext;

    public async Task<Result<VehicleTypeDto>> Handle(CreateVehicleTypeCommand request, CancellationToken ct)
    {
        var vehicleTypeResult = VehicleType.Create(
            Guid.NewGuid(),
            request.Code,
            request.NameEn,
            request.NameAr,
            request.NameNl,
            request.Capacity,
            request.RatePerKm,
            request.RatePerMin,
            request.MinFare,
            request.Currency,
            request.SortOrder);

        if (vehicleTypeResult.IsFailure)
        {
            return vehicleTypeResult.Error;
        }

        var vehicleType = vehicleTypeResult.Value;

        _context.VehicleTypes.Add(vehicleType);
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
