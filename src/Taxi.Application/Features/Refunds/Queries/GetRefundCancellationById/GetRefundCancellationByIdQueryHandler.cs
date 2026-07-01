using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Refunds.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;

namespace Taxi.Application.Features.Refunds.Queries.GetRefundCancellationById;

public sealed class GetRefundCancellationByIdQueryHandler(IAppDbContext context)
    : IRequestHandler<GetRefundCancellationByIdQuery, Result<AdminRefundDetailDto>>
{
    public async Task<Result<AdminRefundDetailDto>> Handle(GetRefundCancellationByIdQuery request, CancellationToken ct)
    {
        var cancellation = await context.TripCancellations
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellation => cancellation.Id == request.TripCancellationId, ct);
        if (cancellation is null)
        {
            return PaymentErrors.NotFound;
        }

        var trackedRefundExists = await context.PaymentRefunds
            .AsNoTracking()
            .AnyAsync(refund => refund.TripCancellationId == cancellation.Id, ct);
        if (trackedRefundExists)
        {
            return PaymentErrors.RefundDuplicate;
        }

        var details = await RefundDtoProjector.ToManualCancellationRefundDetailsAsync(context, [cancellation], ct);
        return details[0];
    }
}
