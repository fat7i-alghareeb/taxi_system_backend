namespace Taxi.Application.Common.Interfaces;

public interface IClientConfigProvider
{
    ClientConfig GetClientConfig();
}

public sealed record ClientConfig(
    bool StripeEnabled,
    string StripePublishableKey,
    bool SignalREnabled);
