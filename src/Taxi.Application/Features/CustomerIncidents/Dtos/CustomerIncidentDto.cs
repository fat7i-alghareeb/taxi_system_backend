namespace Taxi.Application.Features.CustomerIncidents.Dtos;

/// <summary>List/feed item for the admin incident dashboard.</summary>
public record CustomerIncidentDto(
    Guid Id,
    Guid PassengerId,
    string? PassengerName,
    Guid? TripId,
    string? TripReferenceCode,
    string Type,
    string Severity,
    string Status,
    string Title,
    string? Reason,
    decimal? Amount,
    string? CurrencyCode,
    DateTimeOffset CreatedAtUtc,
    string? Notes,
    Guid? ResolvedByAdminId,
    DateTimeOffset? ResolvedAtUtc);

/// <summary>
/// Full incident view with the sensitive, full-access material an admin may inspect:
/// trip audio recordings, in-trip chat (when still retained), and locations.
/// </summary>
public record CustomerIncidentDetailDto(
    CustomerIncidentDto Incident,
    string? PassengerPhone,
    string? TripStatus,
    List<IncidentRecordingDto> Recordings,
    List<IncidentChatMessageDto> ChatMessages,
    List<IncidentLocationDto> Locations);

public record IncidentRecordingDto(
    Guid Id,
    string FileUrl,
    string Type,
    int? DurationSeconds,
    DateTimeOffset RecordedAtUtc);

public record IncidentChatMessageDto(
    string SenderRole,
    string? Content,
    string? PhotoUrl,
    DateTimeOffset SentAtUtc);

public record IncidentLocationDto(
    double Latitude,
    double Longitude,
    string? AddressLabel,
    int Sequence);
