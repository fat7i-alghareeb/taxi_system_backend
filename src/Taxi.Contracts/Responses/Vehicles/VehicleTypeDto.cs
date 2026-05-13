namespace Taxi.Contracts.Responses.Vehicles;

public record VehicleTypeDto(
    Guid Id,
    string Code,
    string Name,
    int Capacity,
    decimal RatePerKm,
    decimal RatePerMin,
    decimal MinFare,
    string Currency,
    int SortOrder);

