using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.SuspendPassenger;

/// <summary>Admin suspends (bans) a passenger account, blocking further sign-in.</summary>
public record SuspendPassengerCommand(Guid UserId, string? Reason) : IRequest<Result<Success>>;
