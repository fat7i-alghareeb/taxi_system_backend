using MediatR;
using Taxi.Application.Features.RefundIssues.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.RefundIssues.Queries.GetRefundIssueById;

public record GetRefundIssueByIdQuery(Guid Id) : IRequest<Result<RefundIssueDto>>;
