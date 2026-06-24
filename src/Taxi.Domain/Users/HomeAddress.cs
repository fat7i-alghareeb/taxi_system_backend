namespace Taxi.Domain.Users;

/// <summary>
/// Optional home address the passenger can set on their profile. Captured via the
/// app's map picker, so it carries a human-readable label plus coordinates.
/// </summary>
public record HomeAddress(string Label, decimal? Latitude, decimal? Longitude);
