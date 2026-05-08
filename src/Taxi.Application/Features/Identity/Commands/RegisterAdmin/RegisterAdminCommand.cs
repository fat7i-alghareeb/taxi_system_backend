using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Identity.Commands.RegisterAdmin;

public record RegisterAdminCommand(
    string Phone,
    string Password,
    string NameEn,
    string NameAr,
    string NameNl,
    string? Email) : IRequest<Result<Guid>>;

