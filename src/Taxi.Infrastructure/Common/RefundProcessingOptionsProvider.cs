using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Taxi.Application.Common.Interfaces;
using Taxi.Infrastructure.Settings;

namespace Taxi.Infrastructure.Common;

public sealed class RefundProcessingOptionsProvider(
    IOptions<AppSettings> appSettings,
    IHostEnvironment environment) : IRefundProcessingOptionsProvider
{
    public RefundProcessingOptions GetOptions()
    {
        var forceFailure = appSettings.Value.Stripe.ForceRefundFailure &&
            !environment.IsProduction();

        return new RefundProcessingOptions(forceFailure);
    }
}
