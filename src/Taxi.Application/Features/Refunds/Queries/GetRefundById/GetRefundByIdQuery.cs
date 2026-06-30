using MediatR;
using Taxi.Application.Features.Refunds.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Refunds.Queries.GetRefundById;

public record GetRefundByIdQuery(Guid RefundId) : IRequest<Result<AdminRefundDetailDto>>;
