using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.UpdateTripPassengerCount;

public record UpdateTripPassengerCountCommand(
    Guid TripId,
    int PassengerCount) : IRequest<Result<TripDto>>;
