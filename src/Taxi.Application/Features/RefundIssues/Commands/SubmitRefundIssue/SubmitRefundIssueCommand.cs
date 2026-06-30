using MediatR;
using Taxi.Application.Features.RefundIssues.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.RefundIssues.Commands.SubmitRefundIssue;

public record SubmitRefundIssueCommand(
    Guid TripId,
    string RequestType,
    string CustomerReason,
    string? Note,
    bool WhatsAppOpened = false)
    : IRequest<Result<RefundIssueDto>>;
