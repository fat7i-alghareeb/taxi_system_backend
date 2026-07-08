using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetTripFinancials;

/// <summary>Admin: the full money breakdown for a trip (fare, fees, wallet/card, unpaid, refunds).</summary>
public record GetTripFinancialsQuery(Guid TripId) : IRequest<Result<TripFinancialsDto>>;
