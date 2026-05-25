using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.DriverCancelTrip;

public record DriverCancelTripCommand(Guid TripId, string Reason, string? Note = null) : IRequest<Result<TripDto>>;
