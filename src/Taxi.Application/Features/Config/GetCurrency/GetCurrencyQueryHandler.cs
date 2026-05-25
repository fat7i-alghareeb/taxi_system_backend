using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;

namespace Taxi.Application.Features.Config.GetCurrency;

public class GetCurrencyQueryHandler(IAppDbContext context)
    : IRequestHandler<GetCurrencyQuery, Result<CurrencyDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<CurrencyDto>> Handle(GetCurrencyQuery request, CancellationToken ct)
    {
        var config = await _context.AppConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == AppConfigKeys.Currency, ct);

        var currency = string.IsNullOrWhiteSpace(config?.Value) ? "EUR" : config!.Value;
        return new CurrencyDto(currency);
    }
}
