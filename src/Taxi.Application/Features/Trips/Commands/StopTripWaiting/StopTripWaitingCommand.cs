using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.StopTripWaiting;

public record StopTripWaitingCommand(Guid TripId) : IRequest<Result<WaitingSessionDto>>;
