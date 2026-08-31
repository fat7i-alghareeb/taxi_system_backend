using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;

namespace Taxi.Application.Features.Config.UpdateAppVersionConfig;

public class UpdateAppVersionConfigCommandHandler(IAppDbContext context, HybridCache cache)
    : IRequestHandler<UpdateAppVersionConfigCommand, Result<AppVersionConfigDto>>
{
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<AppVersionConfigDto>> Handle(UpdateAppVersionConfigCommand request, CancellationToken ct)
    {
        // The validator guarantees Enabled is non-null; default to false so a
        // hypothetical bypass still fails safe (gate off) rather than gate on.
        var enabled = request.Enabled ?? false;

        var androidLatest = request.AndroidLatestVersion?.Trim() ?? string.Empty;
        var androidMinimum = request.AndroidMinimumRequiredVersion?.Trim() ?? string.Empty;
        var androidStoreUrl = request.AndroidStoreUrl?.Trim() ?? string.Empty;
        var iosLatest = request.IosLatestVersion?.Trim() ?? string.Empty;
        var iosMinimum = request.IosMinimumRequiredVersion?.Trim() ?? string.Empty;
        var iosStoreUrl = request.IosStoreUrl?.Trim() ?? string.Empty;

        // The flag is written unconditionally with a literal "true"/"false" so it can
        // never take the blank-clears-the-row path below. Losing this row silently
        // would disable the gate, which is safe, but leaving it undefined is not.
        await UpsertAsync(
            AppConfigKeys.AppUpdateCheckEnabled,
            enabled ? "true" : "false",
            "Master switch for the customer app version gate.",
            ct);

        await UpsertAsync(AppConfigKeys.AndroidLatestVersion, androidLatest, "Latest customer app version published on Google Play.", ct);
        await UpsertAsync(AppConfigKeys.AndroidMinimumRequiredVersion, androidMinimum, "Oldest customer app version allowed on Android.", ct);
        await UpsertAsync(AppConfigKeys.AndroidStoreUrl, androidStoreUrl, "Google Play listing opened by the Android update prompt.", ct);
        await UpsertAsync(AppConfigKeys.IosLatestVersion, iosLatest, "Latest customer app version published on the App Store.", ct);
        await UpsertAsync(AppConfigKeys.IosMinimumRequiredVersion, iosMinimum, "Oldest customer app version allowed on iOS.", ct);
        await UpsertAsync(AppConfigKeys.IosStoreUrl, iosStoreUrl, "App Store listing opened by the iOS update prompt.", ct);

        await _context.SaveChangesAsync(ct);

        // Drop the cached payload immediately. The GET's 60s TTL is only a
        // backstop; an admin raising the minimum version to stop a broken build
        // must take effect on the very next client fetch, not a minute later.
        await _cache.RemoveByTagAsync("app-config", ct);

        return new AppVersionConfigDto(
            Enabled: enabled,
            Android: new PlatformVersionDto(androidLatest, androidMinimum, androidStoreUrl),
            Ios: new PlatformVersionDto(iosLatest, iosMinimum, iosStoreUrl));
    }

    // The AppConfig invariant forbids empty values, so a blank field is modelled
    // as the absence of its key: upsert when set, delete the row when cleared.
    private async Task UpsertAsync(string key, string value, string description, CancellationToken ct)
    {
        var config = await _context.AppConfigs.FirstOrDefaultAsync(c => c.Key == key, ct);

        if (string.IsNullOrWhiteSpace(value))
        {
            if (config is not null)
            {
                _context.AppConfigs.Remove(config);
            }

            return;
        }

        if (config is null)
        {
            // Matches the existing config handlers: a failed Create is skipped rather
            // than surfaced. Values reaching here are already validated, so the guard
            // is unreachable in practice.
            var createResult = AppConfig.Create(key, value, description);
            if (createResult.IsSuccess)
            {
                _context.AppConfigs.Add(createResult.Value);
            }
        }
        else
        {
            config.UpdateValue(value);
        }
    }
}
