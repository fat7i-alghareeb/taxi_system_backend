namespace Taxi.Application.Features.Trips.Dtos;

/// <summary>
/// Result of previewing a proposed destination / passenger edit — the fare
/// difference the customer must confirm before it is applied. The trip is not mutated,
/// but the re-priced quote IS persisted (unused) so <see cref="PreviewToken"/> can be
/// handed back on apply and the delta reproduced exactly, without a second routing call.
/// </summary>
public record TripEditPreviewDto(
    decimal OldFinalFare,
    decimal NewFinalFare,
    decimal Delta,
    string Currency,
    Guid? NewVehicleTypeId,
    string? NewVehicleTypeName,
    int EffectivePassengerCount,
    string Direction, // "charge" | "refund" | "none"
    Guid PreviewToken);
