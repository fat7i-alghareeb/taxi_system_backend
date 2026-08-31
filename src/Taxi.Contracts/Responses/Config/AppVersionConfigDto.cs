namespace Taxi.Contracts.Responses.Config;

/// <summary>
/// Remote version gate configuration for the customer app, read at bootstrap.
/// </summary>
public record AppVersionConfigDto(bool Enabled, PlatformVersionDto Android, PlatformVersionDto Ios);
