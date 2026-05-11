using MediatR;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Config.GetTripDiscount;

public record GetTripDiscountQuery : IRequest<Result<TripDiscountDto>>;
