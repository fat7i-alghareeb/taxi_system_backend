using FluentValidation;

namespace Taxi.Application.Features.Users.Commands.ReactivatePassenger;

public class ReactivatePassengerCommandValidator : AbstractValidator<ReactivatePassengerCommand>
{
    public ReactivatePassengerCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
    }
}
