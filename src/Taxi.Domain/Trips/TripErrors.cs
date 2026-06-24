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

    public static readonly Error ScheduledEnRouteNotReady = Error.Validation(
        code: LocalizationKeys.Trip.ScheduledEnRouteNotReady,
        description: "This scheduled trip can only be marked on the way 15 minutes before its scheduled time.");

    public static readonly Error ScheduledArrivalNotReady = Error.Validation(
        code: LocalizationKeys.Trip.ScheduledArrivalNotReady,
        description: "This scheduled trip cannot be marked arrived before its scheduled time.");

    public static readonly Error ScheduledStartNotReady = Error.Validation(
        code: LocalizationKeys.Trip.ScheduledStartNotReady,
        description: "This scheduled trip cannot be started before its scheduled time.");

    public static readonly Error AlreadyAccepted = Error.Conflict(
        code: LocalizationKeys.Trip.AlreadyAccepted,
        description: "This trip has already been accepted by another admin.");

    public static Error NotAcceptedByCurrentAdmin(string ownerName) => Error.Forbidden(
        code: LocalizationKeys.Trip.NotAcceptedByCurrentAdmin,
        description: $"This ride is owned by another admin with name : {ownerName}.",
        ownerName);

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

    public static readonly Error FlightNumberRequired = Error.Validation(
        code: LocalizationKeys.Trip.FlightNumberRequired,
        description: "A flight number is required for airport pickups.");

    public static readonly Error InvalidFlightNumber = Error.Validation(
        code: LocalizationKeys.Trip.FlightNumberInvalid,
        description: "The flight number format is invalid.");

    public static readonly Error InvalidRating = Error.Validation(
        code: LocalizationKeys.Trip.InvalidRating,
        description: "The rating must be between 1 and 5 stars.");

    public static readonly Error ChatClosed = Error.Validation(
        code: LocalizationKeys.Trip.ChatClosed,
        description: "The chat for this trip is closed.");

    public static readonly Error NotAChatParticipant = Error.Forbidden(
        code: LocalizationKeys.Trip.ChatNotParticipant,
        description: "You are not a participant of this trip's chat.");

    public static readonly Error EmptyMessage = Error.Validation(
        code: LocalizationKeys.Trip.ChatEmptyMessage,
        description: "A message must contain text or a photo.");

    public static readonly Error MessageTooLong = Error.Validation(
        code: LocalizationKeys.Trip.ChatMessageTooLong,
        description: "The message is too long.");
}

