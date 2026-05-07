using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetPassengerTrips;

public record GetPassengerTripsQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedResult<TripSummaryDto>>>;
