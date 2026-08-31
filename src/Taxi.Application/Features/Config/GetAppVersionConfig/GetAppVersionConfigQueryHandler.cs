using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;

namespace Taxi.Application.Features.Config.GetAppVersionConfig;

public class GetAppVersionConfigQueryHandler(IAppDbContext context)
    : IRequestHandler<GetAppVersionConfigQuery, Result<AppVersionConfigDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<AppVersionConfigDto>> Handle(GetAppVersionConfigQuery request, CancellationToken ct)
    {
        var keys = new[]
        {
            AppConfigKeys.AppUpdateCheckEnabled,
            AppConfigKeys.AndroidLatestVersion,
            AppConfigKeys.AndroidMinimumRequiredVersion,
            AppConfigKeys.AndroidStoreUrl,
            AppConfigKeys.IosLatestVersion,
            AppConfigKeys.IosMinimumRequiredVersion,
            AppConfigKeys.IosStoreUrl,
        };

        var values = await _context.AppConfigs
            .AsNoTracking()
            .Where(c => keys.Contains(c.Key))
            .ToDictionaryAsync(c => c.Key, c => c.Value, ct);

        // A missing or unparseable flag reads as false so a lost row disables the
        // gate rather than locking every install out of the app.
        var enabled = string.Equals(
            values.GetValueOrDefault(AppConfigKeys.AppUpdateCheckEnabled),
            "true",
            StringComparison.OrdinalIgnoreCase);

        return new AppVersionConfigDto(
            Enabled: enabled,
            Android: new PlatformVersionDto(
                LatestVersion: values.GetValueOrDefault(AppConfigKeys.AndroidLatestVersion) ?? string.Empty,
                MinimumRequiredVersion: values.GetValueOrDefault(AppConfigKeys.AndroidMinimumRequiredVersion) ?? string.Empty,
                StoreUrl: values.GetValueOrDefault(AppConfigKeys.AndroidStoreUrl) ?? string.Empty),
            Ios: new PlatformVersionDto(
                LatestVersion: values.GetValueOrDefault(AppConfigKeys.IosLatestVersion) ?? string.Empty,
                MinimumRequiredVersion: values.GetValueOrDefault(AppConfigKeys.IosMinimumRequiredVersion) ?? string.Empty,
                StoreUrl: values.GetValueOrDefault(AppConfigKeys.IosStoreUrl) ?? string.Empty));
    }
}
