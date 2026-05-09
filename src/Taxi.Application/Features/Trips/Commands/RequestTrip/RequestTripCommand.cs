using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.RequestTrip;

public record RequestTripCommand(
    Guid QuoteId,
    List<CoordinateDto> Stops,
    decimal PickupLatitude,
    decimal PickupLongitude,
    string? PickupAddress = null,
    string? PickupStreetName = null,
    string? PickupHouseNumber = null,
    DateTimeOffset? ScheduledAt = null) : IRequest<Result<TripDto>>;

