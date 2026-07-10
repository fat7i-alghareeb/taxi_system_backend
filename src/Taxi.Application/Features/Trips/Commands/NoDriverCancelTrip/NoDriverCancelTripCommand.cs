using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.NoDriverCancelTrip;

/// <summary>
/// Passenger cancels a pending trip from the "no driver found" prompt. The server
/// forces a 100% refund (company could not provide a driver) — granted only when it
/// verifies the trip is genuinely awaiting the no-driver decision.
/// </summary>
public record NoDriverCancelTripCommand(Guid TripId, string? Note = null) : IRequest<Result<TripDto>>;
