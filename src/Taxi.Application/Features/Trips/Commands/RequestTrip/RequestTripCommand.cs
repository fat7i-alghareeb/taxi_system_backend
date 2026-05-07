using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.RequestTrip;

public record RequestTripCommand(
    Guid VehicleTypeId,
    Guid QuoteId,
    List<TripStopDto> Stops) : IRequest<Result<TripDto>>;

