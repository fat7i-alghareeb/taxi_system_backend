using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Queries.GetDriverEarnings;

public record DriverEarningsDto(
    int TotalTrips,
    decimal TotalEarnings,
    string CurrencyCode,
    List<DriverTripEarningDto> Trips);

public record DriverTripEarningDto(
    Guid TripId,
    string ReferenceCode,
    decimal Fare,
    string CurrencyCode,
    DateTimeOffset CompletedAt);

public record GetDriverEarningsQuery() : IRequest<Result<DriverEarningsDto>>;
