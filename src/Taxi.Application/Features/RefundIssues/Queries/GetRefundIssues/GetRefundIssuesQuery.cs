using MediatR;
using Taxi.Application.Features.RefundIssues.Dtos;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.RefundIssues.Queries.GetRefundIssues;

public record GetRefundIssuesQuery(
    int Page = 1,
    int PageSize = 20,
    string? ReviewStatus = null,
    string? RequestType = null,
    Guid? TripId = null,
    Guid? PassengerId = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null)
    : IRequest<Result<PagedResult<RefundIssueDto>>>;
