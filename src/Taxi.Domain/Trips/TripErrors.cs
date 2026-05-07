using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Trips;

public static class TripErrors
{
    public static readonly Error QuoteExpired = Error.Validation(
        code: LocalizationKeys.Trip.QuoteExpired,
        description: "The quote has expired. Please request a new one.");

    public static readonly Error InvalidStops = Error.Validation(
        code: LocalizationKeys.Trip.InvalidStops,
        description: "A trip must have at least an origin and a destination.");

    public static Error InvalidStatus(TripStatus status) => Error.Validation(
        code: LocalizationKeys.Trip.InvalidStatus,
        description: $"Cannot perform this action when trip is in '{status}' status.");

    public static readonly Error NotFound = Error.NotFound(
        code: LocalizationKeys.Trip.NotFound,
        description: "Trip not found.");

    public static readonly Error QuoteNotFound = Error.NotFound(
        code: LocalizationKeys.Trip.QuoteNotFound,
        description: "The specified quote was not found.");

    public static readonly Error PassengerNotFound = Error.NotFound(
        code: LocalizationKeys.Trip.PassengerNotFound,
        description: "Passenger not found or is not a passenger.");

    public static readonly Error VehicleTypeNotFound = Error.NotFound(
        code: LocalizationKeys.Trip.VehicleTypeNotFound,
        description: "The specified vehicle type was not found or is inactive.");
}
