using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.UpdateTripStops;

public record UpdateTripStopItem(decimal Latitude, decimal Longitude, string? Label);

public record UpdateTripStopsCommand(
    Guid TripId,
    IReadOnlyList<UpdateTripStopItem> Stops) : IRequest<Result<TripDto>>;
