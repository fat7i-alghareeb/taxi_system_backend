using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Refunds;
using Taxi.Application.Features.Refunds.Dtos;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;

namespace Taxi.Application.Features.Refunds.Queries.GetRefunds;

public sealed class GetRefundsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetRefundsQuery, Result<PagedResult<AdminRefundDetailDto>>>
{
    public async Task<Result<PagedResult<AdminRefundDetailDto>>> Handle(GetRefundsQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var query = context.PaymentRefunds.AsNoTracking();

        if (Enum.TryParse<PaymentRefundStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(refund => refund.Status == status);
        }

        if (Enum.TryParse<PaymentRefundSourceType>(request.SourceType, ignoreCase: true, out var sourceType))
        {
            query = query.Where(refund => refund.SourceType == sourceType);
        }

        if (request.RequiresAdminAction.HasValue)
        {
            query = query.Where(refund => refund.RequiresAdminAction == request.RequiresAdminAction.Value);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(refund => refund.RequestedAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(refund => refund.RequestedAtUtc <= request.ToUtc.Value);
        }

        if (request.TripId is { } tripId && tripId != Guid.Empty)
        {
            query = query.Where(refund => refund.TripId == tripId);
        }

        if (request.PassengerId is { } passengerId && passengerId != Guid.Empty)
        {
            query = query.Where(refund => refund.PassengerId == passengerId);
        }

        if (request.MinAmount.HasValue)
        {
            query = query.Where(refund => refund.Amount >= request.MinAmount.Value);
        }

        if (request.MaxAmount.HasValue)
        {
            query = query.Where(refund => refund.Amount <= request.MaxAmount.Value);
        }

        if (Enum.TryParse<PaymentMethod>(request.PaymentMethod, ignoreCase: true, out var paymentMethod))
        {
            var paymentIds = await context.Payments
                .AsNoTracking()
                .Where(payment => payment.Method == paymentMethod)
                .Select(payment => payment.Id)
                .ToListAsync(ct);
            query = query.Where(refund => paymentIds.Contains(refund.PaymentId));
        }

        var totalCount = await query.CountAsync(ct);

        var refunds = await query
            .OrderByDescending(refund => refund.RequiresAdminAction)
            .ThenByDescending(refund => refund.Status == PaymentRefundStatus.Failed)
            .ThenByDescending(refund => refund.RequestedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = await RefundDtoProjector.ToAdminRefundDetailsAsync(context, refunds, ct);
        return new PagedResult<AdminRefundDetailDto>(items, totalCount, page, pageSize);
    }
}
