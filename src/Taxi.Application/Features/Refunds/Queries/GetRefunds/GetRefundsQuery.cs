using MediatR;
using Taxi.Application.Features.Refunds.Dtos;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Refunds.Queries.GetRefunds;

public record GetRefundsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    string? SourceType = null,
    bool? RequiresAdminAction = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    Guid? TripId = null,
    Guid? PassengerId = null,
    string? PaymentMethod = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null)
    : IRequest<Result<PagedResult<AdminRefundDetailDto>>>;
