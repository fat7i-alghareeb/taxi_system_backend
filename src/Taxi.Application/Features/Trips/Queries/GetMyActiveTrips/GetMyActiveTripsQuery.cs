using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetMyActiveTrips;

/// <summary>
/// Returns every active (non-terminal) trip the calling passenger holds, oldest
/// pickup first. Unlike <c>GetMyActiveTripQuery</c> — which returns a single trip
/// and is what the driver app consumes — this exists because a passenger may hold
/// one live trip plus any number of future reservations at the same time.
/// </summary>
public record GetMyActiveTripsQuery : IRequest<Result<List<TripDto>>>;
