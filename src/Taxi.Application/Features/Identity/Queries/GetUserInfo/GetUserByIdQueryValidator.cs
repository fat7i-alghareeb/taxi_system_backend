using FluentValidation;

namespace Taxi.Application.Features.Identity.Queries.GetUserInfo;

public class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdQueryValidator()
    {
        RuleFor(q => q.UserId)
            .NotEmpty();
    }
}
