using MediatR;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Config.GetCurrency;

public record GetCurrencyQuery : IRequest<Result<CurrencyDto>>;
