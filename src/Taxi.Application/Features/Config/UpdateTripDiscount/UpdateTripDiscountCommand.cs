using MediatR;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Config.UpdateTripDiscount;

public record UpdateTripDiscountCommand(decimal DiscountPercent) : IRequest<Result<TripDiscountDto>>;
