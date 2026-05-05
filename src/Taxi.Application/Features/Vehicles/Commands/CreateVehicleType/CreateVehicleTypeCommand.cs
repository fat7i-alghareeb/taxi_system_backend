using MediatR;
using Taxi.Application.Features.Vehicles.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Commands.CreateVehicleType;

public record CreateVehicleTypeCommand(
    string Code,
    string NameEn,
    string NameAr,
    string NameNl,
    int Capacity,
    decimal RatePerKm,
    decimal RatePerMin,
    decimal MinFare,
    string Currency = "EUR",
    int SortOrder = 0) : IRequest<Result<VehicleTypeDto>>;
