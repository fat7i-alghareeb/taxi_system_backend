using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.AdminTakeTrip;

/// <summary>
/// Lets an authenticated admin atomically accept ownership of a paid trip.
/// No backing Driver record is created; admin ownership is stored directly on the trip.
/// </summary>
public record AdminTakeTripCommand(Guid TripId) : IRequest<Result<Success>>;
