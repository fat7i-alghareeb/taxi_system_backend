namespace Taxi.Domain.Trips;

public enum CancellationReason
{
    PassengerWithinOneHour = 0,
    DriverLateClaim = 1,
    PassengerLate = 2,
    PassengerNoShow = 3,
    PassengerUnreachable = 4,
    AdminOverride = 5,
}
