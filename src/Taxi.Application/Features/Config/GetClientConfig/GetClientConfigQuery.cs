using MediatR;

using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Config.GetClientConfig;

public record GetClientConfigQuery : IRequest<Result<ClientConfigDto>>;
