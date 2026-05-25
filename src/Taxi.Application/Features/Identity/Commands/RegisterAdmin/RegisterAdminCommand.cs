using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Identity.Commands.RegisterAdmin;

public record RegisterAdminCommand(
    string Phone,
    string Password,
    string Name,
    string? Email) : IRequest<Result<Guid>>;

