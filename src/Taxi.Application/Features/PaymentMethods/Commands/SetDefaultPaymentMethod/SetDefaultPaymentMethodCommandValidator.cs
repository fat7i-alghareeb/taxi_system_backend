using FluentValidation;

namespace Taxi.Application.Features.PaymentMethods.Commands.SetDefaultPaymentMethod;

public class SetDefaultPaymentMethodCommandValidator : AbstractValidator<SetDefaultPaymentMethodCommand>
{
    public SetDefaultPaymentMethodCommandValidator()
    {
        RuleFor(v => v.PaymentMethodId).NotEmpty();
    }
}
