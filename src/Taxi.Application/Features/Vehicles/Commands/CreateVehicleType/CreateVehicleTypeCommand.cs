using MediatR;
using Taxi.Contracts.Responses.Vehicles;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Vehicles.Commands.CreateVehicleType;

public record CreateVehicleTypeCommand(
    string Code,
    string NameEn,
    string NameAr,
    string NameNl,
    string NameDe,
    string NamePl,
    string NameUk,
    string NameFr,
    string NameEs,
    string NameRo,
    int Capacity,
    decimal RatePerKm,
    decimal RatePerMin,
    decimal MinFare,
    string Currency = "EUR",
    int SortOrder = 0) : IRequest<Result<VehicleTypeDto>>;

