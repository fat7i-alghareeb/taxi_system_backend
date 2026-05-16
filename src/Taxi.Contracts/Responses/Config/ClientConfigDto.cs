namespace Taxi.Contracts.Responses.Config;

public record ClientConfigDto(
    bool StripeEnabled,
    string StripePublishableKey,
    bool SignalREnabled);
