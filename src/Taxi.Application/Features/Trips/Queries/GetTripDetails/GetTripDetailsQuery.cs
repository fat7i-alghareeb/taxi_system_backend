using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetTripDetails;

public record GetTripDetailsQuery(Guid TripId) : IRequest<Result<TripDetailsDto>>;

public record TripDetailsDto(
    Guid TripId,
    string ReferenceCode,
    Guid PassengerId,
    string PassengerPhone,
    string PassengerName,
    Guid? DriverId,
    string? DriverName,
    string? DriverPhone,
    Guid VehicleTypeId,
    string VehicleTypeName,
    string Status,
    decimal Fare,
    string CurrencyCode,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ScheduledAtUtc,
    DateTimeOffset? AssignedAtUtc,
    DateTimeOffset? ArrivedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    List<TripStopDto> Stops,
    CancellationPolicyDto? Cancellation = null,
    CompensationClaimDto? CompensationClaim = null,
    WaitingSessionDto? ActiveWaitingSession = null,

    // Route geometry mirrors TripDto so the admin dashboard can draw the same
    // multi-stop polyline the customer/driver apps render from /trips/{id}.
    string? EncodedOverviewPolyline = null,
    List<TripRouteSegmentDto>? RouteSegments = null,
    string? PassengerNote = null);
