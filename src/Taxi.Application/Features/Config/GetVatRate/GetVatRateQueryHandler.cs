using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;

namespace Taxi.Application.Features.Config.GetVatRate;

public class GetVatRateQueryHandler(IAppDbContext context)
    : IRequestHandler<GetVatRateQuery, Result<VatRateDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<VatRateDto>> Handle(GetVatRateQuery request, CancellationToken ct)
    {
        var config = await _context.AppConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == AppConfigKeys.VatRate, ct);

        var rate = decimal.TryParse(config?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var r)
            ? r
            : 0m;
        return new VatRateDto(rate);
    }
}
