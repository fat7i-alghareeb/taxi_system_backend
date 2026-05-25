using FluentValidation;

namespace Taxi.Application.Features.Config.UpdateCurrency;

public class UpdateCurrencyCommandValidator : AbstractValidator<UpdateCurrencyCommand>
{
    public UpdateCurrencyCommandValidator()
    {
        RuleFor(x => x.CurrencyCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$")
            .WithErrorCode("Currency.Invalid");
    }
}
