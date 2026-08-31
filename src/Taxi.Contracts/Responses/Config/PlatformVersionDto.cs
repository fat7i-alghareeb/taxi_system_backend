namespace Taxi.Contracts.Responses.Config;

/// <summary>
/// Version gate rules for a single mobile platform. Empty strings mean "not
/// configured" — the client treats an unconfigured platform as ungated.
/// </summary>
public record PlatformVersionDto(string LatestVersion, string MinimumRequiredVersion, string StoreUrl);
