namespace Taxi.Contracts.Requests.Config;

/// <summary>
/// Version gate rules for a single platform. A null or blank field clears the
/// stored value, because the AppConfig invariant forbids empty values.
/// </summary>
public record PlatformVersionRequest(string? LatestVersion, string? MinimumRequiredVersion, string? StoreUrl);
