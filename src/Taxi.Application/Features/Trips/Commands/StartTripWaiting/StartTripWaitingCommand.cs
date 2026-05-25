using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.StartTripWaiting;

public record StartTripWaitingCommand(Guid TripId) : IRequest<Result<WaitingSessionDto>>;
