using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Common;

/// <summary>
/// Which statuses allow a customer destination / passenger edit (with fare
/// re-pricing). Mirrors <c>Trip.IsEditableForRepricing</c> for the application-layer
/// guards (preview / apply). Editable from acceptance through arrival, never once
/// the ride is in progress or terminal.
/// </summary>
public static class TripEditPolicy
{
    public static bool IsEditableForRepricing(TripStatus status) =>
        status is TripStatus.AwaitingAdminAcceptance
            or TripStatus.Accepted
            or TripStatus.EnRoute
            or TripStatus.Arrived;
}
