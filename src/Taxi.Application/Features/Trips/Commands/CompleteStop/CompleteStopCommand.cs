using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.CompleteStop;

/// <summary>
/// Marks an intermediate trip stop as completed while a trip is in progress.
/// Final dropoff completion uses <c>CompleteTripCommand</c> instead.
/// </summary>
public record CompleteStopCommand(Guid TripId, int Sequence) : IRequest<Result<Success>>;
