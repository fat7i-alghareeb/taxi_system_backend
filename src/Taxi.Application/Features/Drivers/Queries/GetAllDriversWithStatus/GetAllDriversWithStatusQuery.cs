using MediatR;

using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Queries.GetAllDriversWithStatus;

public record DriverWithStatusDto(
    Guid DriverId,
    string Name,
    string Phone,
    string Status,
    string ApprovalStatus,
    double? Latitude,
    double? Longitude,
    DateTimeOffset? LocationUpdatedAt,
    Guid? VehicleTypeId,
    string? VehicleTypeName);

public record GetAllDriversWithStatusQuery() : IRequest<Result<List<DriverWithStatusDto>>>;