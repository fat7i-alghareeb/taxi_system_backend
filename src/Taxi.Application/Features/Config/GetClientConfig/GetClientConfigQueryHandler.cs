using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Config.GetClientConfig;

public class GetClientConfigQueryHandler(IClientConfigProvider provider)
    : IRequestHandler<GetClientConfigQuery, Result<ClientConfigDto>>
{
    public Task<Result<ClientConfigDto>> Handle(GetClientConfigQuery request, CancellationToken ct)
    {
        var config = provider.GetClientConfig();
        Result<ClientConfigDto> result = new ClientConfigDto(
            config.StripeEnabled,
            config.StripePublishableKey,
            config.SignalREnabled);
        return Task.FromResult(result);
    }
}
