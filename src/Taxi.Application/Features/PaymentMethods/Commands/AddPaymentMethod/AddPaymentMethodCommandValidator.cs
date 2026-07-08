using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.PaymentMethods.Commands.AddPaymentMethod;

public class AddPaymentMethodCommandValidator : AbstractValidator<AddPaymentMethodCommand>
{
    public AddPaymentMethodCommandValidator()
    {
        RuleFor(v => v.PaymentMethodId)
            .NotEmpty()
            .WithErrorCode(LocalizationKeys.PassengerPaymentMethod.GatewayPaymentMethodIdRequired)
            .WithMessage(LocalizationKeys.PassengerPaymentMethod.GatewayPaymentMethodIdRequired);
    }
}
