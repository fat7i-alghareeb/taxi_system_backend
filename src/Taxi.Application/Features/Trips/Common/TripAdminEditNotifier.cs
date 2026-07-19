using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Common;

/// <summary>
/// What the customer changed on an in-flight trip. Selects the notification body and is echoed to
/// the client in the push payload as <c>changeKind</c>.
/// </summary>
public enum TripEditKind
{
    Route,
    Passengers,
    Bags,
}

/// <summary>
/// Tells the admin responsible for a trip that the customer changed it. Without this, an admin who
/// has already taken a trip has no way to learn the route or party size moved under them.
/// </summary>
public interface ITripAdminEditNotifier
{
    Task NotifyAsync(Trip trip, TripEditKind kind, CancellationToken ct = default);
}

public sealed class TripAdminEditNotifier(
    INotificationService notificationService,
    ILogger<TripAdminEditNotifier> logger) : ITripAdminEditNotifier
{
    public async Task NotifyAsync(Trip trip, TripEditKind kind, CancellationToken ct = default)
    {
        // A dropped push must never fail the edit the customer just paid for.
        try
        {
            var (bodyKey, bodyArgs) = kind switch
            {
                TripEditKind.Route => (
                    LocalizationKeys.Notification.TripEditedRouteBody,
                    new object[] { trip.ReferenceCode }),
                TripEditKind.Passengers => (
                    LocalizationKeys.Notification.TripEditedPassengersBody,
                    new object[] { trip.ReferenceCode, trip.PassengerCount }),
                TripEditKind.Bags => (
                    LocalizationKeys.Notification.TripEditedBagsBody,
                    new object[] { trip.ReferenceCode, trip.BagCount }),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };

            var data = new Dictionary<string, string>
            {
                ["type"] = "trip_edited",
                ["tripId"] = trip.Id.ToString(),
                ["changeKind"] = kind.ToString().ToLowerInvariant(),
                ["status"] = trip.Status.ToString(),
            };

            if (trip.AcceptedByAdminId is { } ownerAdminId)
            {
                await notificationService.SendPushNotificationToAdminAsync(
                    ownerAdminId,
                    LocalizationKeys.Notification.TripEditedByPassengerTitle,
                    bodyKey,
                    data,
                    ct,
                    bodyArgs: bodyArgs);
            }
            else
            {
                // Nobody owns the trip yet, so there is no single admin to address — fan out so
                // whoever picks it up sees the current details.
                await notificationService.SendPushNotificationToAdminsAsync(
                    LocalizationKeys.Notification.TripEditedByPassengerTitle,
                    bodyKey,
                    data,
                    ct,
                    bodyArgs: bodyArgs);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to notify admins about {Kind} change on trip {TripId}.",
                kind,
                trip.Id);
        }
    }
}
