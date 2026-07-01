using MediatR;
using Taxi.Application.Features.Refunds.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Refunds.Queries.GetRefundCancellationById;

public record GetRefundCancellationByIdQuery(Guid TripCancellationId) : IRequest<Result<AdminRefundDetailDto>>;
