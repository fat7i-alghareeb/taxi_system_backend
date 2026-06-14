using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetMyActiveTrip;

/// <summary>
/// Returns the calling user's current active (non-terminal) trip, or null when
/// there is none. Works for both the passenger (their trip) and the assigned
/// driver (their assignment).
/// </summary>
public record GetMyActiveTripQuery : IRequest<Result<TripDto?>>;
