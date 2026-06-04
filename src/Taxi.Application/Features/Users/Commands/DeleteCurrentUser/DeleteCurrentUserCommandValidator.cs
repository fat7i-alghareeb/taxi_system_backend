using FluentValidation;

namespace Taxi.Application.Features.Users.Commands.DeleteCurrentUser;

// Parameter-less command — no fields to validate. The validator exists to keep the
// command-and-validator pairing uniform across the codebase (architectural guard).
public class DeleteCurrentUserCommandValidator : AbstractValidator<DeleteCurrentUserCommand>
{
}
