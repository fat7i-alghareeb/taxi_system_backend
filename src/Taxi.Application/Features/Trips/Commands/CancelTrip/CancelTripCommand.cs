using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.CancelTrip;

public record CancelTripCommand(Guid TripId, string? Reason = null, string? Note = null) : IRequest<Result<TripDto>>;

