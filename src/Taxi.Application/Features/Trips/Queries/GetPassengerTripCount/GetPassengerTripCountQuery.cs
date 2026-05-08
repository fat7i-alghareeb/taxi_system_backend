using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetPassengerTripCount;

public record GetPassengerTripCountQuery() : IRequest<Result<int>>;

