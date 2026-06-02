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

    public static readonly Error QuoteAlreadyUsed = Error.Conflict(
        code: LocalizationKeys.Trip.QuoteAlreadyUsed,
        description: "This quote has already been used.");

    public static readonly Error DriverNotFound = Error.NotFound(
        code: LocalizationKeys.Trip.DriverNotFound,
        description: "No available driver found to assign to this trip.");

    public static readonly Error ScheduledAtTooSoon = Error.Validation(
        code: LocalizationKeys.Trip.ScheduledAtTooSoon,
        description: "Scheduled time must be at least 15 minutes in the future.");

    public static readonly Error CannotCancel = Error.Validation(
        code: LocalizationKeys.Trip.CannotCancel,
        description: "This trip cannot be cancelled in its current status.");

    public static readonly Error CancellationWindowExpired = Error.Validation(
        code: LocalizationKeys.Trip.CancellationWindowExpired,
        description: "Passenger cancellation is only available within 1 hour of booking.");

    public static readonly Error DriverCancelTooEarly = Error.Validation(
        code: LocalizationKeys.Trip.DriverCancelTooEarly,
        description: "Driver cancellation is only available after arriving and waiting at least 10 minutes.");

    public static readonly Error InvalidCancellationReason = Error.Validation(
        code: LocalizationKeys.Trip.InvalidCancellationReason,
        description: "The cancellation reason is not valid for this action.");

    public static readonly Error CompensationClaimNoteRequired = Error.Validation(
        code: LocalizationKeys.Trip.CompensationClaimNoteRequired,
        description: "A compensation claim requires a note explaining the delay proof.");

    public static readonly Error CompensationClaimAlreadyReviewed = Error.Validation(
        code: LocalizationKeys.Trip.CompensationClaimAlreadyReviewed,
        description: "This compensation claim has already been reviewed.");

    public static readonly Error ActiveWaitingSessionExists = Error.Validation(
        code: LocalizationKeys.Trip.ActiveWaitingSessionExists,
        description: "This trip already has an active waiting session.");

    public static readonly Error ActiveWaitingSessionNotFound = Error.NotFound(
        code: LocalizationKeys.Trip.ActiveWaitingSessionNotFound,
        description: "No active waiting session was found for this trip.");

    public static readonly Error NotOwnedByPassenger = Error.Validation(
        code: LocalizationKeys.Trip.NotOwnedByPassenger,
        description: "This trip does not belong to the current passenger.");

    public static Error StopNotFound(int sequence) => Error.NotFound(
        code: LocalizationKeys.Trip.StopNotFound,
        description: $"Trip stop with sequence '{sequence}' was not found.");

    public static Error StopOutOfOrder(int sequence) => Error.Validation(
        code: LocalizationKeys.Trip.StopOutOfOrder,
        description: $"Cannot complete stop '{sequence}' because earlier stops are not yet completed.");

    public static Error StopAlreadyCompleted(int sequence) => Error.Validation(
        code: LocalizationKeys.Trip.StopAlreadyCompleted,
        description: $"Trip stop with sequence '{sequence}' has already been completed.");

    public static readonly Error PendingStopsRemaining = Error.Validation(
        code: LocalizationKeys.Trip.PendingStopsRemaining,
        description: "Cannot complete the trip while intermediate stops are still pending.");

    public static readonly Error TooManyStops = Error.Validation(
        code: LocalizationKeys.Trip.TooManyStops,
        description: "A trip cannot have more than 25 stops.");

    public static readonly Error CannotUpdatePassengerNote = Error.Validation(
        code: LocalizationKeys.Trip.CannotUpdatePassengerNote,
        description: "Passenger note cannot be updated after the trip has started.");
}

