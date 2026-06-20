namespace Taxi.Application.Features.Trips.Dtos;

/// <summary>
/// A single in-trip chat message as returned to clients. <see cref="SenderRole"/>
/// is the string name of <c>TripMessageSenderRole</c> ("Passenger"/"Driver"/"Admin").
/// </summary>
public record TripMessageDto(
    Guid Id,
    Guid TripId,
    Guid SenderId,
    string SenderRole,
    string? Content,
    string? PhotoUrl,
    DateTimeOffset SentAtUtc);
