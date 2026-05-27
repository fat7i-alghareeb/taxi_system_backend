using MediatR;
using Taxi.Application.Features.Admins.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Admins.Commands.UpdateCurrentAdminProfile;

public record UpdateCurrentAdminProfileCommand(
    string Name,
    string Email,
    string? Phone1,
    string? Phone2) : IRequest<Result<AdminProfileDto>>;
