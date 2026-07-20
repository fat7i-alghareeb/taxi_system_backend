using MediatR;

using Taxi.Application.Features.Trips.Common;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.ApplyTripEdit;

/// <summary>
/// Applies a previewed destination / passenger edit and settles the fare difference.
/// With a <paramref name="PreviewToken"/> the previewed quote is reused, so the delta is
/// reproduced exactly. Without one it re-quotes live and guards against drift from
/// <paramref name="ExpectedDelta"/>.
/// </summary>
public sealed record ApplyTripEditCommand(
    Guid TripId,
    IReadOnlyList<TripEditStop>? Stops,
    int? PassengerCount,
    decimal ExpectedDelta,
    Guid? PreviewToken = null) : IRequest<Result<TripEditApplyResultDto>>;
