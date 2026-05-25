namespace Taxi.Contracts.Requests.Trips;

public sealed record SubmitCompensationClaimRequest(string Note, List<string>? EvidenceUrls = null);
