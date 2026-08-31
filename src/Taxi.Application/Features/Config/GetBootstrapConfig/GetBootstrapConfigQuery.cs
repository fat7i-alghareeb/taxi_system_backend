using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Config.GetBootstrapConfig;

/// <summary>
/// Single startup payload for the customer app.
/// </summary>
/// <remarks>
/// Cached on the same "app-config" tag as the individual config slices, so the
/// existing <c>RemoveByTagAsync("app-config")</c> calls in the update handlers
/// invalidate this too. The TTL is only a backstop.
///
/// <c>IsCultureAware</c> is false: none of the payload is translated, so a
/// culture-aware key would fragment one entry across nine locales for nothing.
/// </remarks>
public record GetBootstrapConfigQuery : ICachedQuery<Result<BootstrapConfigDto>>
{
    public string CacheKey => "bootstrap-config";

    public string[] Tags => ["app-config"];

    public TimeSpan Expiration => TimeSpan.FromSeconds(60);

    public bool IsCultureAware => false;
}
