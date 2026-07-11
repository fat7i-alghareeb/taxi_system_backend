using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.PostponeNoDriverSearch;

public class PostponeNoDriverSearchCommandValidator : AbstractValidator<PostponeNoDriverSearchCommand>
{
    public PostponeNoDriverSearchCommandValidator()
    {
        RuleFor(c => c.TripId).NotEmpty();
    }
}
