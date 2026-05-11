using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;

namespace Taxi.Application.Features.Config.UpdateTripDiscount;

public class UpdateTripDiscountCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdateTripDiscountCommand, Result<TripDiscountDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<TripDiscountDto>> Handle(UpdateTripDiscountCommand request, CancellationToken ct)
    {
        var discountPercentValue = request.DiscountPercent.ToString();

        var config = await _context.AppConfigs
            .FirstOrDefaultAsync(c => c.Key == AppConfigKeys.TripDiscountPercent, ct);

        if (config is null)
        {
            var createResult = AppConfig.Create(AppConfigKeys.TripDiscountPercent, discountPercentValue);
            if (createResult.IsFailure)
            {
                return createResult.Error;
            }

            config = createResult.Value;
            _context.AppConfigs.Add(config);
        }
        else
        {
            var updateResult = config.UpdateValue(discountPercentValue);
            if (updateResult.IsFailure)
            {
                return updateResult.Error;
            }
        }

        await _context.SaveChangesAsync(ct);
        return new TripDiscountDto(request.DiscountPercent);
    }
}
