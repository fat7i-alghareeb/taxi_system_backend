namespace Taxi.Application.Features.Trips.Dtos;

public record CancellationPolicyDto(
    string Actor,
    string Reason,
    decimal RefundPercent,
    decimal RefundAmount,
    string CurrencyCode,
    string? Note,
    DateTimeOffset CreatedAtUtc);

public record CompensationClaimDto(
    Guid Id,
    Guid TripId,
    Guid PassengerId,
    string Note,
    List<string> EvidenceUrls,
    decimal RequestedAmount,
    string CurrencyCode,
    string Status,
    string? ReviewNotes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc);

public record WaitingSessionDto(
    Guid Id,
    Guid TripId,
    Guid DriverId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? StoppedAtUtc,
    int? Minutes,
    decimal? EstimatedFee,
    bool IsActive,
    decimal RatePerMinute,
    int GraceMinutes,
    int? BillableMinutes);
