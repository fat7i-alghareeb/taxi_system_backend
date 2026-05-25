namespace Taxi.Contracts.Requests.Trips;

public sealed record CancelTripRequest(string? Reason = null, string? Note = null);
