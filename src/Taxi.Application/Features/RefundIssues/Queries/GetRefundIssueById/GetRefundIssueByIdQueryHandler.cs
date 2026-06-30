using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.RefundIssues;
using Taxi.Application.Features.RefundIssues.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.RefundIssues;

namespace Taxi.Application.Features.RefundIssues.Queries.GetRefundIssueById;

public sealed class GetRefundIssueByIdQueryHandler(IAppDbContext context)
    : IRequestHandler<GetRefundIssueByIdQuery, Result<RefundIssueDto>>
{
    public async Task<Result<RefundIssueDto>> Handle(GetRefundIssueByIdQuery request, CancellationToken ct)
    {
        var issue = await context.RefundIssues
            .AsNoTracking()
            .FirstOrDefaultAsync(issue => issue.Id == request.Id, ct);
        if (issue is null)
        {
            return RefundIssueErrors.NotFound;
        }

        var items = await RefundIssueDtoProjector.ToDtosAsync(context, [issue], ct);
        return items[0];
    }
}
