namespace Taxi.Contracts.Requests.Trips;

public sealed record DriverCancelTripRequest(string Reason, string? Note = null);
