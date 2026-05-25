using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.AssignDriverToTrip;

public record AssignDriverToTripCommand(Guid TripId, Guid DriverId) : IRequest<Result<Success>>;
