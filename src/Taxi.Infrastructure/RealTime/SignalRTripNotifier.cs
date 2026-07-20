using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Notifications;
using Taxi.Infrastructure.Hubs;

namespace Taxi.Infrastructure.RealTime;

/// <summary>
/// SignalR implementation of ITripNotifier.
/// Pushes trip lifecycle events to connected clients using targeted groups.
/// Payloads are strongly-typed records from <c>Taxi.Contracts.Notifications</c>.
/// </summary>
/// <remarks>
/// Passenger-facing events are sent to BOTH the per-trip group <c>Trip_{tripId}</c>
/// (joined on demand while the active-trip screen is open) AND the per-user group
/// <c>User_{passengerId}</c> (always joined on connect in <see cref="TripHub.OnConnectedAsync"/>).
/// The per-user channel guarantees delivery to the passenger's connection even if the
/// app never joined the trip group — which is what lets the client open the rating sheet
/// globally on completion.
/// </remarks>
public sealed class SignalRTripNotifier(IHubContext<TripHub> hubContext, ILogger<SignalRTripNotifier> logger)
    : ITripNotifier
{
    private readonly IHubContext<TripHub> _hubContext = hubContext;
    private readonly ILogger<SignalRTripNotifier> _logger = logger;

    public Task NotifyTripRequestedAsync(Guid tripId, Guid vehicleTypeId, Guid passengerId, CancellationToken ct = default, Guid? eventId = null)
    {
        var payload = new TripRequestedNotification(tripId, vehicleTypeId, passengerId, ResolveEventId(eventId));

        _logger.LogInformation(
            "[SignalRTripNotifier] => TripRequested groups=VehicleType_{VehicleTypeId},Admins tripId={TripId} passengerId={PassengerId}",
            vehicleTypeId,
            tripId,
            passengerId);

        // Notify the matching drivers (subscribed to this vehicle type) AND every admin.
        return Task.WhenAll(
            _hubContext.Clients.Group($"VehicleType_{vehicleTypeId}").SendAsync("TripRequested", payload, ct),
            _hubContext.Clients.Group("Admins").SendAsync("TripRequested", payload, ct));
    }

    public Task NotifyTripAwaitingAdminAcceptanceAsync(
        Guid tripId,
        Guid vehicleTypeId,
        Guid passengerId,
        DateTimeOffset? scheduledAtUtc,
        CancellationToken ct = default,
        Guid? eventId = null)
    {
        var payload = new TripAwaitingAdminAcceptanceNotification(
            tripId,
            vehicleTypeId,
            passengerId,
            scheduledAtUtc,
            ResolveEventId(eventId));

        return _hubContext.Clients.Group(TripHub.AdminsGroup)
            .SendAsync("TripAwaitingAdminAcceptance", payload, ct);
    }

    public Task NotifyAdminAcceptedAsync(
        Guid tripId,
        Guid passengerId,
        Guid adminId,
        CancellationToken ct = default,
        Guid? eventId = null)
    {
        var payload = new AdminAcceptedTripNotification(
            tripId,
            passengerId,
            adminId,
            ResolveEventId(eventId));

        return Task.WhenAll(
            SendToPassengerAsync("TripAccepted", tripId, passengerId, payload, ct),
            _hubContext.Clients.Group(TripHub.AdminsGroup).SendAsync("TripAccepted", payload, ct));
    }

    public Task NotifyDriverAssignedAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default, Guid? eventId = null) =>
        SendLifecycleAsync("DriverAssigned", tripId, passengerId, new DriverAssignedNotification(tripId, passengerId, driverId, ResolveEventId(eventId)), ct);

    public Task NotifyDriverEnRouteAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default, Guid? eventId = null) =>
        SendLifecycleAsync("DriverEnRoute", tripId, passengerId, new DriverEnRouteNotification(tripId, passengerId, driverId, ResolveEventId(eventId)), ct);

    public Task NotifyDriverArrivedAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default, Guid? eventId = null) =>
        SendLifecycleAsync("DriverArrived", tripId, passengerId, new DriverArrivedNotification(tripId, passengerId, driverId, ResolveEventId(eventId)), ct);

    public Task NotifyTripStartedAsync(
        Guid tripId,
        Guid passengerId,
        Guid? driverUserId,
        CancellationToken ct = default,
        Guid? eventId = null)
    {
        var payload = new TripStartedNotification(tripId, passengerId, ResolveEventId(eventId));
        var sends = new List<Task>
        {
            _hubContext.Clients.Group($"Trip_{tripId}").SendAsync("TripStarted", payload, ct),
            _hubContext.Clients.Group($"User_{passengerId}").SendAsync("TripStarted", payload, ct),
            _hubContext.Clients.Group(TripHub.AdminsGroup).SendAsync("TripStarted", payload, ct),
        };
        if (driverUserId is { } driver)
        {
            sends.Add(_hubContext.Clients.Group($"User_{driver}").SendAsync("TripStarted", payload, ct));
        }

        return Task.WhenAll(sends);
    }

    public Task NotifyTripCompletedAsync(Guid tripId, Guid passengerId, CancellationToken ct = default, Guid? eventId = null) =>
        SendLifecycleAsync("TripCompleted", tripId, passengerId, new TripCompletedNotification(tripId, passengerId, ResolveEventId(eventId)), ct);

    public Task NotifyTripCancelledAsync(Guid tripId, Guid passengerId, CancellationToken ct = default, Guid? eventId = null) =>
        SendToPassengerAsync("TripCancelled", tripId, passengerId, new TripCancelledNotification(tripId, passengerId, ResolveEventId(eventId)), ct);

    public Task NotifyNoDriverFoundAsync(Guid tripId, Guid passengerId, CancellationToken ct = default, Guid? eventId = null) =>
        SendToPassengerAsync("NoDriverFound", tripId, passengerId, new NoDriverFoundNotification(tripId, passengerId, ResolveEventId(eventId)), ct);

    public Task NotifyTripDestinationChangedAsync(
        Guid tripId,
        Guid passengerId,
        Guid? driverUserId,
        Guid? driverId,
        decimal newDropoffLatitude,
        decimal newDropoffLongitude,
        string? newDropoffLabel,
        CancellationToken ct = default,
        Guid? eventId = null)
    {
        var payload = new TripDestinationChangedNotification(
            tripId,
            passengerId,
            driverId,
            newDropoffLatitude,
            newDropoffLongitude,
            newDropoffLabel,
            ResolveEventId(eventId));

        _logger.LogInformation(
            "[SignalRTripNotifier] => TripDestinationChanged groups=Trip_{TripId},User_{PassengerId},User_{DriverUserId},Admins tripId={TripId}",
            tripId,
            passengerId,
            driverUserId?.ToString() ?? "(none)",
            tripId);

        var sends = new List<Task>
        {
            _hubContext.Clients.Group($"Trip_{tripId}").SendAsync("TripDestinationChanged", payload, ct),
            _hubContext.Clients.Group($"User_{passengerId}").SendAsync("TripDestinationChanged", payload, ct),
            _hubContext.Clients.Group(TripHub.AdminsGroup).SendAsync("TripDestinationChanged", payload, ct),
        };

        if (driverUserId is { } driver)
        {
            sends.Add(_hubContext.Clients.Group($"User_{driver}").SendAsync("TripDestinationChanged", payload, ct));
        }

        return Task.WhenAll(sends);
    }

    public Task NotifyWalletBalanceChangedAsync(
        WalletBalanceChangedNotification payload,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[SignalRTripNotifier] => WalletBalanceChanged group=User_{UserId} balance={Balance} owed={AmountOwed}",
            payload.UserId,
            payload.Balance,
            payload.AmountOwed);

        return _hubContext.Clients
            .Group($"User_{payload.UserId}")
            .SendAsync("WalletBalanceChanged", payload, ct);
    }

    public Task NotifyTripEditAppliedAsync(
        TripEditAppliedNotification payload,
        Guid? driverUserId,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[SignalRTripNotifier] => TripEditApplied groups=Trip_{TripId},User_{PassengerId},Admins tripId={TripId} delta={Delta} newFare={NewFare}",
            payload.TripId,
            payload.PassengerId,
            payload.TripId,
            payload.Delta,
            payload.NewFare);

        var sends = new List<Task>
        {
            _hubContext.Clients.Group($"Trip_{payload.TripId}").SendAsync("TripEditApplied", payload, ct),
            _hubContext.Clients.Group($"User_{payload.PassengerId}").SendAsync("TripEditApplied", payload, ct),
            _hubContext.Clients.Group(TripHub.AdminsGroup).SendAsync("TripEditApplied", payload, ct),
        };

        if (driverUserId is { } driver)
        {
            sends.Add(_hubContext.Clients.Group($"User_{driver}").SendAsync("TripEditApplied", payload, ct));
        }

        return Task.WhenAll(sends);
    }

    public Task NotifyTripCancelledToDriverAsync(Guid tripId, Guid driverUserId, Guid passengerId, CancellationToken ct = default, Guid? eventId = null)
    {
        _logger.LogInformation(
            "[SignalRTripNotifier] => TripCancelled group=User_{DriverUserId} tripId={TripId}",
            driverUserId,
            tripId);

        return _hubContext.Clients
            .Group($"User_{driverUserId}")
            .SendAsync("TripCancelled", new TripCancelledNotification(tripId, passengerId, ResolveEventId(eventId)), ct);
    }

    public Task NotifyTripCancelledToAdminsAsync(Guid tripId, Guid passengerId, CancellationToken ct = default, Guid? eventId = null)
    {
        _logger.LogInformation(
            "[SignalRTripNotifier] => TripCancelled group={AdminsGroup} tripId={TripId}",
            TripHub.AdminsGroup,
            tripId);

        return _hubContext.Clients
            .Group(TripHub.AdminsGroup)
            .SendAsync("TripCancelled", new TripCancelledNotification(tripId, passengerId, ResolveEventId(eventId)), ct);
    }

    public Task NotifyPaymentConfirmedAsync(Guid tripId, Guid passengerId, CancellationToken ct = default, Guid? eventId = null) =>
        SendToPassengerAsync("PaymentConfirmed", tripId, passengerId, new PaymentConfirmedNotification(tripId, passengerId, ResolveEventId(eventId)), ct);

    public Task NotifyPaymentFailedAsync(Guid tripId, Guid passengerId, string reason, CancellationToken ct = default, Guid? eventId = null) =>
        SendToPassengerAsync("PaymentFailed", tripId, passengerId, new PaymentFailedNotification(tripId, passengerId, reason, ResolveEventId(eventId)), ct);

    public Task NotifyTripRefundedAsync(Guid tripId, Guid passengerId, decimal amount, CancellationToken ct = default, Guid? eventId = null) =>
        SendToPassengerAsync("TripRefunded", tripId, passengerId, new TripRefundedNotification(tripId, passengerId, amount, ResolveEventId(eventId)), ct);

    public Task NotifyRefundLifecycleChangedAsync(
        Guid refundId,
        Guid paymentId,
        Guid? tripId,
        Guid? passengerId,
        string status,
        decimal amount,
        string currency,
        bool requiresAdminAction,
        bool canRetry,
        string sourceType,
        CancellationToken ct = default,
        Guid? eventId = null)
    {
        var payload = new RefundLifecycleChangedNotification(
            refundId,
            paymentId,
            tripId,
            passengerId,
            status,
            amount,
            currency,
            requiresAdminAction,
            canRetry,
            sourceType,
            ResolveEventId(eventId));

        _logger.LogInformation(
            "[SignalRTripNotifier] => RefundLifecycleChanged groups=Admins,Trip_{TripId},User_{PassengerId} refundId={RefundId} status={Status}",
            tripId?.ToString() ?? "(none)",
            passengerId?.ToString() ?? "(none)",
            refundId,
            status);

        var sends = new List<Task>
        {
            _hubContext.Clients.Group(TripHub.AdminsGroup).SendAsync("RefundLifecycleChanged", payload, ct),
        };
        if (tripId is { } relatedTripId)
        {
            sends.Add(_hubContext.Clients.Group($"Trip_{relatedTripId}").SendAsync("RefundLifecycleChanged", payload, ct));
        }
        if (passengerId is { } relatedPassengerId)
        {
            sends.Add(_hubContext.Clients.Group($"User_{relatedPassengerId}").SendAsync("RefundLifecycleChanged", payload, ct));
        }

        return Task.WhenAll(sends);
    }

    public Task NotifyRefundIssueCreatedToAdminsAsync(
        Guid refundIssueId,
        Guid tripId,
        Guid passengerId,
        Guid? paymentId,
        string requestType,
        string reviewStatus,
        CancellationToken ct = default,
        Guid? eventId = null)
    {
        var payload = new RefundIssueCreatedNotification(
            refundIssueId,
            tripId,
            passengerId,
            paymentId,
            requestType,
            reviewStatus,
            ResolveEventId(eventId));

        _logger.LogInformation(
            "[SignalRTripNotifier] => RefundIssueCreated group={AdminsGroup} refundIssueId={RefundIssueId} tripId={TripId}",
            TripHub.AdminsGroup,
            refundIssueId,
            tripId);

        return _hubContext.Clients
            .Group(TripHub.AdminsGroup)
            .SendAsync("RefundIssueCreated", payload, ct);
    }

    public Task NotifyTripStopCompletedAsync(Guid tripId, Guid passengerId, Guid? driverId, int sequence, CancellationToken ct = default, Guid? eventId = null) =>
        SendLifecycleAsync("TripStopCompleted", tripId, passengerId, new TripStopCompletedNotification(tripId, passengerId, driverId, sequence, ResolveEventId(eventId)), ct);

    public Task NotifyTripMessageAsync(TripMessageNotification message, Guid passengerId, Guid? driverUserId, CancellationToken ct = default) =>
        SendToChatParticipantsAsync("TripMessageReceived", message.TripId, passengerId, driverUserId, message, ct);

    public Task NotifyChatClosedAsync(Guid tripId, Guid passengerId, Guid? driverUserId, CancellationToken ct = default) =>
        SendToChatParticipantsAsync("ChatClosed", tripId, passengerId, driverUserId, new ChatClosedNotification(tripId), ct);

    public Task NotifyCustomerIncidentRaisedToAdminsAsync(
        Guid incidentId,
        Guid passengerId,
        Guid? tripId,
        string type,
        string severity,
        CancellationToken ct = default,
        Guid? eventId = null)
    {
        var payload = new CustomerIncidentRaisedNotification(
            incidentId,
            passengerId,
            tripId,
            type,
            severity,
            ResolveEventId(eventId));

        _logger.LogInformation(
            "[SignalRTripNotifier] => CustomerIncidentRaised group={AdminsGroup} incidentId={IncidentId} type={Type} severity={Severity}",
            TripHub.AdminsGroup,
            incidentId,
            type,
            severity);

        return _hubContext.Clients
            .Group(TripHub.AdminsGroup)
            .SendAsync("CustomerIncidentRaised", payload, ct);
    }

    /// <summary>
    /// Fans a chat event out to the per-trip group, the passenger's and (if assigned)
    /// the driver's per-user groups, and all admins — so every open chat surface updates.
    /// </summary>
    private Task SendToChatParticipantsAsync(
        string eventName,
        Guid tripId,
        Guid passengerId,
        Guid? driverUserId,
        object payload,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[SignalRTripNotifier] => {EventName} groups=Trip_{TripId},User_{PassengerId},User_{DriverUserId},Admins tripId={TripId}",
            eventName,
            tripId,
            passengerId,
            driverUserId?.ToString() ?? "(none)",
            tripId);

        var sends = new List<Task>
        {
            _hubContext.Clients.Group($"Trip_{tripId}").SendAsync(eventName, payload, ct),
            _hubContext.Clients.Group($"User_{passengerId}").SendAsync(eventName, payload, ct),
            _hubContext.Clients.Group(TripHub.AdminsGroup).SendAsync(eventName, payload, ct),
        };

        if (driverUserId is { } driver)
        {
            sends.Add(_hubContext.Clients.Group($"User_{driver}").SendAsync(eventName, payload, ct));
        }

        return Task.WhenAll(sends);
    }

    /// <summary>
    /// Sends a passenger-facing event to both the per-trip group and the passenger's
    /// always-on per-user group, logging the emit and target groups first.
    /// </summary>
    private Task SendToPassengerAsync(string eventName, Guid tripId, Guid passengerId, object payload, CancellationToken ct)
    {
        _logger.LogInformation(
            "[SignalRTripNotifier] => {EventName} groups=Trip_{TripId},User_{PassengerId} tripId={TripId} passengerId={PassengerId}",
            eventName,
            tripId,
            passengerId,
            tripId,
            passengerId);

        return Task.WhenAll(
            _hubContext.Clients.Group($"Trip_{tripId}").SendAsync(eventName, payload, ct),
            _hubContext.Clients.Group($"User_{passengerId}").SendAsync(eventName, payload, ct));
    }

    private Task SendLifecycleAsync(string eventName, Guid tripId, Guid passengerId, object payload, CancellationToken ct)
    {
        _logger.LogInformation(
            "[SignalRTripNotifier] => {EventName} groups=Trip_{TripId},User_{PassengerId},Admins tripId={TripId}",
            eventName,
            tripId,
            passengerId,
            tripId);

        return Task.WhenAll(
            _hubContext.Clients.Group($"Trip_{tripId}").SendAsync(eventName, payload, ct),
            _hubContext.Clients.Group($"User_{passengerId}").SendAsync(eventName, payload, ct),
            _hubContext.Clients.Group(TripHub.AdminsGroup).SendAsync(eventName, payload, ct));
    }

    private static Guid ResolveEventId(Guid? eventId) =>
        eventId is { } value && value != Guid.Empty ? value : Guid.NewGuid();
}
