namespace Taxi.Application.Features.Drivers.Dtos;

public record DriverDto(
    Guid Id,
    Guid UserId,
    string? FullName,
    string LicenseNumber,
    string Status,
    Guid? ActiveVehicleId);
