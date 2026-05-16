using Microsoft.Extensions.Options;

using Taxi.Application.Common.Interfaces;
using Taxi.Infrastructure.Settings;

namespace Taxi.Infrastructure.Common;

public sealed class ClientConfigProvider(IOptions<AppSettings> appSettings) : IClientConfigProvider
{
    public ClientConfig GetClientConfig()
    {
        var settings = appSettings.Value;
        return new ClientConfig(
            StripeEnabled: settings.Features.StripeEnabled,
            StripePublishableKey: settings.Stripe.PublishableKey ?? string.Empty,
            SignalREnabled: settings.Features.SignalREnabled);
    }
}
