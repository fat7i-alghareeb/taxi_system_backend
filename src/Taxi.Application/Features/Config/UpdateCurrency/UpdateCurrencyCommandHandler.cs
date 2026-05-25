using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;

namespace Taxi.Application.Features.Config.UpdateCurrency;

public class UpdateCurrencyCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdateCurrencyCommand, Result<CurrencyDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<CurrencyDto>> Handle(UpdateCurrencyCommand request, CancellationToken ct)
    {
        var normalized = request.CurrencyCode.Trim().ToUpperInvariant();

        var config = await _context.AppConfigs
            .FirstOrDefaultAsync(c => c.Key == AppConfigKeys.Currency, ct);

        if (config is null)
        {
            var createResult = AppConfig.Create(AppConfigKeys.Currency, normalized);
            if (createResult.IsFailure)
            {
                return createResult.Error;
            }

            config = createResult.Value;
            _context.AppConfigs.Add(config);
        }
        else
        {
            var updateResult = config.UpdateValue(normalized);
            if (updateResult.IsFailure)
            {
                return updateResult.Error;
            }
        }

        await _context.SaveChangesAsync(ct);
        return new CurrencyDto(normalized);
    }
}
