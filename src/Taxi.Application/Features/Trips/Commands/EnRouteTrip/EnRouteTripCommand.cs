using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.EnRouteTrip;

public record EnRouteTripCommand(Guid TripId, bool ForceScheduledOverride = false) : IRequest<Result<Success>>;
