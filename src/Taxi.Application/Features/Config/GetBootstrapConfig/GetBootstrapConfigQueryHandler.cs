using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;

namespace Taxi.Application.Features.Config.GetBootstrapConfig;

/// <summary>
/// Composes the three startup payloads with a single database round trip.
/// </summary>
/// <remarks>
/// Deliberately reads the AppConfig rows once for both the support contact and
/// the version gate rather than delegating to the individual handlers, which
/// would issue two separate queries. Client config is appsettings-backed and
/// costs nothing.
/// </remarks>
public class GetBootstrapConfigQueryHandler(
    IAppDbContext context,
    IClientConfigProvider provider)
    : IRequestHandler<GetBootstrapConfigQuery, Result<BootstrapConfigDto>>
{
    private readonly IAppDbContext _context = context;
    private readonly IClientConfigProvider _provider = provider;

    public async Task<Result<BootstrapConfigDto>> Handle(GetBootstrapConfigQuery request, CancellationToken ct)
    {
        var keys = new[]
        {
            AppConfigKeys.SupportWhatsApp,
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

        var clientConfig = _provider.GetClientConfig();

        // A missing or unparseable flag reads as false so a lost row disables the
        // update gate rather than locking every install out of the app.
        var updateCheckEnabled = string.Equals(
            values.GetValueOrDefault(AppConfigKeys.AppUpdateCheckEnabled),
            "true",
            StringComparison.OrdinalIgnoreCase);

        return new BootstrapConfigDto(
            Client: new ClientConfigDto(
                clientConfig.StripeEnabled,
                clientConfig.StripePublishableKey,
                clientConfig.SignalREnabled),
            Support: new SupportContactDto(
                WhatsApp: values.GetValueOrDefault(AppConfigKeys.SupportWhatsApp) ?? string.Empty),
            AppVersion: new AppVersionConfigDto(
                Enabled: updateCheckEnabled,
                Android: new PlatformVersionDto(
                    LatestVersion: values.GetValueOrDefault(AppConfigKeys.AndroidLatestVersion) ?? string.Empty,
                    MinimumRequiredVersion: values.GetValueOrDefault(AppConfigKeys.AndroidMinimumRequiredVersion) ?? string.Empty,
                    StoreUrl: values.GetValueOrDefault(AppConfigKeys.AndroidStoreUrl) ?? string.Empty),
                Ios: new PlatformVersionDto(
                    LatestVersion: values.GetValueOrDefault(AppConfigKeys.IosLatestVersion) ?? string.Empty,
                    MinimumRequiredVersion: values.GetValueOrDefault(AppConfigKeys.IosMinimumRequiredVersion) ?? string.Empty,
                    StoreUrl: values.GetValueOrDefault(AppConfigKeys.IosStoreUrl) ?? string.Empty)));
    }
}
