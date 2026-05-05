namespace Taxi.Application.Features.Vehicles.Dtos;

public record VehicleTypeDto(
    Guid Id,
    string Code,
    string Name,
    int Capacity,
    decimal RatePerKm,
    decimal RatePerMin,
    decimal MinFare,
    string Currency);
