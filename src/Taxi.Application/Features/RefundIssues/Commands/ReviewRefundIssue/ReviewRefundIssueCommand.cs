using MediatR;
using Taxi.Application.Features.RefundIssues.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.RefundIssues.Commands.ReviewRefundIssue;

public record ReviewRefundIssueCommand(Guid Id, string ReviewStatus, string? AdminNotes)
    : IRequest<Result<RefundIssueDto>>;
