using MediatR;

using Taxi.Application.Features.Trips.Common;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.ApplyTripEdit;

/// <summary>
/// Applies a previewed destination / passenger edit and settles the fare difference.
/// Re-quotes server-side and guards against drift from <paramref name="ExpectedDelta"/>.
/// </summary>
public sealed record ApplyTripEditCommand(
    Guid TripId,
    IReadOnlyList<TripEditStop>? Stops,
    int? PassengerCount,
    decimal ExpectedDelta) : IRequest<Result<TripEditApplyResultDto>>;
