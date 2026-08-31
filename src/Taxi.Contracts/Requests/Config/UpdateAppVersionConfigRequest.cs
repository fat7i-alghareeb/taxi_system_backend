namespace Taxi.Contracts.Requests.Config;

/// <summary>
/// Updates the customer app version gate configuration.
/// </summary>
/// <remarks>
/// <see cref="Enabled"/> is nullable on purpose: with a non-nullable bool a body
/// that omits the field would deserialize to false and silently disable the whole
/// gate. Nullable plus a NotNull rule turns that mistake into a 400 instead.
/// </remarks>
public record UpdateAppVersionConfigRequest(
    bool? Enabled,
    PlatformVersionRequest? Android,
    PlatformVersionRequest? Ios);
