using MediatR;
using Taxi.Application.Features.Drivers.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Commands.CreateDriver;

public record CreateDriverCommand(
    string Phone,
    string NameEn,
    string NameAr,
    string NameNl,
    string NameDe,
    string NamePl,
    string NameUk,
    string NameFr,
    string NameEs,
    string NameRo,
    string LicenseNumber) : IRequest<Result<DriverDto>>;

