using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Refunds;
using Taxi.Application.Features.Refunds.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;

namespace Taxi.Application.Features.Refunds.Queries.GetRefundById;

public sealed class GetRefundByIdQueryHandler(IAppDbContext context, IClientConfigProvider? clientConfigProvider = null)
    : IRequestHandler<GetRefundByIdQuery, Result<AdminRefundDetailDto>>
{
    public async Task<Result<AdminRefundDetailDto>> Handle(GetRefundByIdQuery request, CancellationToken ct)
    {
        var refund = await context.PaymentRefunds
            .AsNoTracking()
            .FirstOrDefaultAsync(refund => refund.Id == request.RefundId, ct);

        if (refund is null)
        {
            return PaymentErrors.NotFound;
        }

        var details = await RefundDtoProjector.ToAdminRefundDetailsAsync(
            context,
            [refund],
            ct,
            clientConfigProvider?.GetClientConfig().StripeEnabled == false);
        return details[0];
    }
}
