using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Admins.Commands.ChangeAdminPassword;

public record ChangeAdminPasswordCommand(
    string CurrentPassword,
    string NewPassword) : IRequest<Result<Success>>;
