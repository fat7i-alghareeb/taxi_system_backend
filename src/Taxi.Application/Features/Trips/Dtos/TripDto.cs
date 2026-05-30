namespace Taxi.Application.Features.Trips.Dtos;

public record TripDto(
    Guid Id,
    string ReferenceCode,
    Guid PassengerId,
    Guid? DriverId,
    Guid VehicleTypeId,
    string Status,
    decimal QuotedFare,
    string CurrencyCode,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ScheduledAtUtc,
    List<TripStopDto> Stops,
    StripePaymentDto? StripePayment = null,
    double? DriverLatitude = null,
    double? DriverLongitude = null,
    string? VehicleTypeName = null,
    CancellationPolicyDto? Cancellation = null,
    CompensationClaimDto? CompensationClaim = null,
    WaitingSessionDto? ActiveWaitingSession = null,
    string? PassengerName = null,
    string? PassengerPhone = null,
    DateTimeOffset? AssignedAtUtc = null,
    DateTimeOffset? ArrivedAtUtc = null,
    DateTimeOffset? StartedAtUtc = null,
    DateTimeOffset? CompletedAtUtc = null,
    string? EncodedOverviewPolyline = null,
    List<TripRouteSegmentDto>? RouteSegments = null);

public record TripRouteSegmentDto(
    int DistanceMeters,
    int DurationSeconds,
    string EncodedPolyline,
    decimal StartLatitude,
    decimal StartLongitude,
    decimal EndLatitude,
    decimal EndLongitude);
