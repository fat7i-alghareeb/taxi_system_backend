using MediatR;

using Taxi.Application.Features.Trips.Common;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.PreviewTripEdit;

/// <summary>
/// Re-prices a proposed destination / passenger edit and returns the fare
/// difference — WITHOUT mutating the trip. The customer confirms the delta before
/// <c>ApplyTripEditCommand</c> commits it.
/// </summary>
public record PreviewTripEditCommand(
    Guid TripId,
    IReadOnlyList<TripEditStop>? Stops,
    int? PassengerCount) : IRequest<Result<TripEditPreviewDto>>;
