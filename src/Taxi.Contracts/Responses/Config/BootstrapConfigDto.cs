namespace Taxi.Contracts.Responses.Config;

/// <summary>
/// Everything the customer app needs at startup, in one request.
/// </summary>
/// <remarks>
/// Replaces three separate calls (client config, support contact, app version).
/// The individual endpoints are kept — the admin app uses them, and they are the
/// fallback if this one fails.
/// </remarks>
public record BootstrapConfigDto(
    ClientConfigDto Client,
    SupportContactDto Support,
    AppVersionConfigDto AppVersion);
