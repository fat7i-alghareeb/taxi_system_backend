namespace Taxi.Contracts.Notifications;

/// <summary>
/// Pushed over SignalR (<c>TripHub</c>) when a new in-trip chat message is sent.
/// The record name is the hub method name on the client (<c>"TripMessageReceived"</c>).
/// Keep in sync with the Flutter <c>RealtimeEvent.tripMessageReceived</c> variant.
/// </summary>
public sealed record TripMessageNotification(
    Guid TripId,
    Guid MessageId,
    Guid SenderId,
    string SenderRole,
    string? Content,
    string? PhotoUrl,
    DateTimeOffset SentAtUtc);

/// <summary>
/// Pushed over SignalR when a trip's chat is closed (trip completed or cancelled).
/// Clients lock the chat input. Method name on the client is <c>"ChatClosed"</c>.
/// </summary>
public sealed record ChatClosedNotification(Guid TripId);
