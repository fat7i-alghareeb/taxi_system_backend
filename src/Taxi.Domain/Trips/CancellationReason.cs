namespace Taxi.Domain.Trips;

public enum CancellationReason
{
    /// <summary>Legacy — see <see cref="PassengerWithinFiveMinutes"/>.</summary>
    PassengerWithinOneHour = 0,
    DriverLateClaim = 1,
    PassengerLate = 2,
    PassengerNoShow = 3,
    PassengerUnreachable = 4,
    AdminOverride = 5,

    /// <summary>Legacy — see <see cref="PassengerAfterFiveMinutes"/>.</summary>
    PassengerAfterOneHour = 6,

    /// <summary>
    /// Airport trip: driver declined to keep waiting after the free 30-minute window.
    /// The trip is cancelled and the passenger is refunded <see cref="CancellationPolicy.DriverCancelRefundPercent"/>%.
    /// </summary>
    AirportWaitDeclined = 7,

    /// <summary>
    /// Legacy: passenger cancelled after the driver had already arrived at the pickup point.
    /// Previously charged a flat fee; now handled by <see cref="PassengerAfterFiveMinutes"/>.
    /// </summary>
    PassengerCancelledAfterArrival = 8,

    /// <summary>
    /// Passenger cancelled within the 5-minute free window from booking creation.
    /// Full refund (<see cref="CancellationPolicy.WithinWindowRefundPercent"/>%) applies.
    /// </summary>
    PassengerWithinFiveMinutes = 9,

    /// <summary>
    /// Passenger cancelled after the 5-minute free window. Only
    /// <see cref="CancellationPolicy.AfterWindowRefundPercent"/>% of the fare is refunded.
    /// </summary>
    PassengerAfterFiveMinutes = 10,
}
