using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Config.GetAppVersionConfig;

/// <summary>
/// Read by every customer app on every cold start, so it is cached — but with a
/// short TTL and explicit invalidation on write.
/// </summary>
/// <remarks>
/// The TTL is only a backstop: <c>UpdateAppVersionConfigCommandHandler</c> drops
/// the "app-config" tag as soon as an admin saves, so raising the minimum version
/// to stop a broken build still takes effect immediately. Without that
/// invalidation a cache here would turn an emergency lever into a delayed one.
///
/// <c>IsCultureAware</c> is false on purpose: the payload carries no translated
/// text, so a culture-aware key would fragment one entry across nine locales for
/// no benefit.
/// </remarks>
public record GetAppVersionConfigQuery : ICachedQuery<Result<AppVersionConfigDto>>
{
    public string CacheKey => "app-version-config";

    public string[] Tags => ["app-config"];

    public TimeSpan Expiration => TimeSpan.FromSeconds(60);

    public bool IsCultureAware => false;
}
