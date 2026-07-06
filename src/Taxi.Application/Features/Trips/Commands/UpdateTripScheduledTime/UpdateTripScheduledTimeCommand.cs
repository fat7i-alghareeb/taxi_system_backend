using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.UpdateTripScheduledTime;

public record UpdateTripScheduledTimeCommand(
    Guid TripId,
    DateTimeOffset? ScheduledAtUtc) : IRequest<Result<Success>>;
