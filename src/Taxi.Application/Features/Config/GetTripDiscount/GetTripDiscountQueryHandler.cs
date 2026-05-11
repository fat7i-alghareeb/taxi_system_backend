using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;

namespace Taxi.Application.Features.Config.GetTripDiscount;

public class GetTripDiscountQueryHandler(IAppDbContext context)
    : IRequestHandler<GetTripDiscountQuery, Result<TripDiscountDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<TripDiscountDto>> Handle(GetTripDiscountQuery request, CancellationToken ct)
    {
        var config = await _context.AppConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == AppConfigKeys.TripDiscountPercent, ct);

        var percent = decimal.TryParse(config?.Value, out var d) ? d : 0m;
        return new TripDiscountDto(percent);
    }
}
