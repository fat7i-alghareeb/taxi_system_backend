using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Drivers.Queries.GetDriverById;

public class GetDriverByIdQueryValidator : AbstractValidator<GetDriverByIdQuery>
{
    public GetDriverByIdQueryValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage(LocalizationKeys.Validation.RequiredField);
    }
}

