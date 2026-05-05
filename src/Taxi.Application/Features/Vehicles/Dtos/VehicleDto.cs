namespace Taxi.Application.Features.Vehicles.Dtos;

public record VehicleDto(
    Guid Id,
    Guid VehicleTypeId,
    Guid DriverId,
    string Make,
    string Model,
    string Year,
    string Color,
    string LicensePlate,
    bool IsActive);
