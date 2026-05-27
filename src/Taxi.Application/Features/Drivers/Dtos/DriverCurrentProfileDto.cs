namespace Taxi.Application.Features.Drivers.Dtos;

public record DriverCurrentProfileDto(
    Guid UserId,
    Guid DriverId,
    string Name,
    string? Email,
    string Phone,
    string? ProfilePhotoUrl,
    string LicenseNumber,
    string ApprovalStatus,
    Guid? VehicleTypeId,
    string? VehicleTypeName);
