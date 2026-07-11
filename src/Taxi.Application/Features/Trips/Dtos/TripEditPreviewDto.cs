namespace Taxi.Application.Features.Trips.Dtos;

/// <summary>
/// Result of previewing a proposed destination / passenger edit — the fare
/// difference the customer must confirm before it is applied. Nothing is mutated.
/// </summary>
public record TripEditPreviewDto(
    decimal OldFinalFare,
    decimal NewFinalFare,
    decimal Delta,
    string Currency,
    Guid? NewVehicleTypeId,
    string? NewVehicleTypeName,
    int EffectivePassengerCount,
    string Direction); // "charge" | "refund" | "none"
