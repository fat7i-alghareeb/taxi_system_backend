using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Trips;

public static class TripErrors
{
    public static readonly Error QuoteExpired = Error.Validation(
        code: "Trip.QuoteExpired",
        description: "The quote has expired. Please request a new one.");

    public static readonly Error InvalidStops = Error.Validation(
        code: "Trip.InvalidStops",
        description: "A trip must have at least an origin and a destination.");

    public static Error InvalidStatus(TripStatus status) => Error.Validation(
        code: "Trip.InvalidStatus",
        description: $"Cannot perform this action when trip is in '{status}' status.");

    public static readonly Error NotFound = Error.NotFound(
        code: "Trip.NotFound",
        description: "Trip not found.");

    public static readonly Error QuoteNotFound = Error.NotFound(
        code: "Trip.QuoteNotFound",
        description: "The specified quote was not found.");

    public static readonly Error PassengerNotFound = Error.NotFound(
        code: "Trip.PassengerNotFound",
        description: "Passenger not found or is not a passenger.");

    public static readonly Error VehicleTypeNotFound = Error.NotFound(
        code: "Trip.VehicleTypeNotFound",
        description: "The specified vehicle type was not found or is inactive.");

    public static readonly Error QuoteAlreadyUsed = Error.Conflict(
        code: "Trip.QuoteAlreadyUsed",
        description: "This quote has already been used.");

    public static readonly Error DriverNotFound = Error.NotFound(
        code: "Trip.DriverNotFound",
        description: "No available driver found to assign to this trip.");

    public static readonly Error ScheduledAtTooSoon = Error.Validation(
        code: "Trip.ScheduledAtTooSoon",
        description: "Scheduled time must be at least 15 minutes in the future.");

    public static readonly Error CannotCancel = Error.Validation(
        code: "Trip.CannotCancel",
        description: "This trip cannot be cancelled in its current status.");

    public static readonly Error NotOwnedByPassenger = Error.Validation(
        code: "Trip.NotOwnedByPassenger",
        description: "This trip does not belong to the current passenger.");
}

