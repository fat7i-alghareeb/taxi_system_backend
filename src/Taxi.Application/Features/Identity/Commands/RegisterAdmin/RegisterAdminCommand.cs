using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Identity.Commands.RegisterAdmin;

public record RegisterAdminCommand(
    string UserName,
    string Password,
    string Name,
    string Email,
    string? Phone1,
    string? Phone2) : IRequest<Result<Guid>>;

