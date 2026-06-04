using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Users.Queries.GetAllUsers;

public class GetAllUsersQueryValidator : AbstractValidator<GetAllUsersQuery>
{
    public GetAllUsersQueryValidator()
    {
        RuleFor(q => q.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode(LocalizationKeys.Validation.PageInvalid);

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100)
            .WithErrorCode(LocalizationKeys.Validation.PageSizeInvalid);
    }
}
