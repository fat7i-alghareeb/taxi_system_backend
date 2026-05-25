using MediatR;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Config.UpdateCurrency;

public record UpdateCurrencyCommand(string CurrencyCode) : IRequest<Result<CurrencyDto>>;
