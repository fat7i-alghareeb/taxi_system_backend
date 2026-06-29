namespace Taxi.Domain.Trips;

public enum CancellationReason
{
    PassengerWithinOneHour = 0,
    DriverLateClaim = 1,
    PassengerLate = 2,
    PassengerNoShow = 3,
    PassengerUnreachable = 4,
    AdminOverride = 5,

    /// <summary>
    /// Passenger cancelled after the free 1-hour window. Per policy the trip is still
    /// cancellable, but only 20% of the fare is refunded.
    /// </summary>
    PassengerAfterOneHour = 6,

    /// <summary>
    /// Airport trip: driver declined to keep waiting after the free 30-minute window.
    /// The trip is cancelled and the passenger is refunded 20%.
    /// </summary>
    AirportWaitDeclined = 7,

    /// <summary>
    /// Passenger cancelled after the driver had already arrived at the pickup point.
    /// A flat fee (see <see cref="CancellationPolicy.ArrivedCancellationFee"/>) is charged;
    /// the remainder of the fare is refunded.
    /// </summary>
    PassengerCancelledAfterArrival = 8,
}
