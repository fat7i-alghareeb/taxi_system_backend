using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Identity.Commands.RegisterAdmin;

public record RegisterAdminCommand(
    string Phone,
    string Password,
    string NameEn,
    string NameAr,
    string NameNl,
    string NameDe,
    string NamePl,
    string NameUk,
    string NameFr,
    string NameEs,
    string NameRo,
    string? Email) : IRequest<Result<Guid>>;

