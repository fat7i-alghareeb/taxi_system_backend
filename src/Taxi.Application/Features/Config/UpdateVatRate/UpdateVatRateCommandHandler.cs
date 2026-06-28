using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;

namespace Taxi.Application.Features.Config.UpdateVatRate;

public class UpdateVatRateCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdateVatRateCommand, Result<VatRateDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<VatRateDto>> Handle(UpdateVatRateCommand request, CancellationToken ct)
    {
        // Persist with the invariant culture so the stored value always uses a
        // '.' decimal separator and round-trips regardless of server locale.
        var rateValue = request.Rate.ToString(CultureInfo.InvariantCulture);

        var config = await _context.AppConfigs
            .FirstOrDefaultAsync(c => c.Key == AppConfigKeys.VatRate, ct);

        if (config is null)
        {
            var createResult = AppConfig.Create(AppConfigKeys.VatRate, rateValue);
            if (createResult.IsFailure)
            {
                return createResult.Error;
            }

            config = createResult.Value;
            _context.AppConfigs.Add(config);
        }
        else
        {
            var updateResult = config.UpdateValue(rateValue);
            if (updateResult.IsFailure)
            {
                return updateResult.Error;
            }
        }

        await _context.SaveChangesAsync(ct);
        return new VatRateDto(request.Rate);
    }
}
