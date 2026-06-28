using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.ReactivatePassenger;

/// <summary>Admin lifts a passenger suspension, restoring sign-in.</summary>
public record ReactivatePassengerCommand(Guid UserId) : IRequest<Result<Success>>;
