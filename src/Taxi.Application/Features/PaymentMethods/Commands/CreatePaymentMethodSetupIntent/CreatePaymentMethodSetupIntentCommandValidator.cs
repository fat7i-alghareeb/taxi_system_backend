using FluentValidation;

namespace Taxi.Application.Features.PaymentMethods.Commands.CreatePaymentMethodSetupIntent;

// No user input to validate; declared to satisfy the command-validator coverage guard.
public class CreatePaymentMethodSetupIntentCommandValidator
    : AbstractValidator<CreatePaymentMethodSetupIntentCommand>
{
}
