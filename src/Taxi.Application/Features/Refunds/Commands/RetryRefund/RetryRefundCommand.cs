using MediatR;
using Taxi.Application.Features.Refunds.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Refunds.Commands.RetryRefund;

public record RetryRefundCommand(Guid RefundId, string? Note) : IRequest<Result<AdminRefundDetailDto>>;
