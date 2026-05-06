namespace Taxi.Contracts.Responses.SavedLocations;

public record SavedLocationResponse(
    Guid Id,
    decimal Latitude,
    decimal Longitude,
    string Label,
    string? PrimaryName,
    string? SecondaryAddress,
    bool IsPinned);
