namespace Taxi.Contracts.Requests.Trips;

public sealed record ReviewCompensationClaimRequest(bool Approved, string? Notes = null);
