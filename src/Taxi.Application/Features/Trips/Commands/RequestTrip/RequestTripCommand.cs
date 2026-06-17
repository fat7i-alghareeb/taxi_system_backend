using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.RequestTrip;

public record RequestTripCommand(
    Guid QuoteId,
    List<CoordinateDto> Stops,
    DateTimeOffset? ScheduledAt = null,
    string? PassengerNote = null,
    bool IsAirport = false) : IRequest<Result<TripDto>>;

