using MediatR;
using Taxi.Application.Features.Admins.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Admins.Queries.GetCurrentAdminProfile;

public record GetCurrentAdminProfileQuery : IRequest<Result<AdminProfileDto>>;
