using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.RefundIssues;
using Taxi.Application.Features.RefundIssues.Dtos;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.RefundIssues;

namespace Taxi.Application.Features.RefundIssues.Queries.GetRefundIssues;

public sealed class GetRefundIssuesQueryHandler(IAppDbContext context)
    : IRequestHandler<GetRefundIssuesQuery, Result<PagedResult<RefundIssueDto>>>
{
    public async Task<Result<PagedResult<RefundIssueDto>>> Handle(GetRefundIssuesQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var query = context.RefundIssues.AsNoTracking();

        if (Enum.TryParse<RefundIssueReviewStatus>(request.ReviewStatus, ignoreCase: true, out var reviewStatus))
        {
            query = query.Where(issue => issue.ReviewStatus == reviewStatus);
        }

        if (Enum.TryParse<RefundIssueRequestType>(request.RequestType, ignoreCase: true, out var requestType))
        {
            query = query.Where(issue => issue.RequestType == requestType);
        }

        if (request.TripId is { } tripId && tripId != Guid.Empty)
        {
            query = query.Where(issue => issue.TripId == tripId);
        }

        if (request.PassengerId is { } passengerId && passengerId != Guid.Empty)
        {
            query = query.Where(issue => issue.PassengerId == passengerId);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(issue => issue.CreatedAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(issue => issue.CreatedAtUtc <= request.ToUtc.Value);
        }

        var totalCount = await query.CountAsync(ct);
        var issues = await query
            .OrderByDescending(issue => issue.ReviewStatus == RefundIssueReviewStatus.Open)
            .ThenByDescending(issue => issue.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = await RefundIssueDtoProjector.ToDtosAsync(context, issues, ct);
        return new PagedResult<RefundIssueDto>(items, totalCount, page, pageSize);
    }
}
