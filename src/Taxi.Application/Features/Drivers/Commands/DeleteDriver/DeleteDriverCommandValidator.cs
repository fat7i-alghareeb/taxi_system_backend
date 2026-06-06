using FluentValidation;

namespace Taxi.Application.Features.Drivers.Commands.DeleteDriver;

public class DeleteDriverCommandValidator : AbstractValidator<DeleteDriverCommand>
{
    public DeleteDriverCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEmpty();
    }
}
