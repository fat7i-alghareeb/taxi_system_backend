using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Payments.Commands.HandleStripeWebhook;

public class HandleStripeWebhookCommandValidator : AbstractValidator<HandleStripeWebhookCommand>
{
    public HandleStripeWebhookCommandValidator()
    {
        // Cryptographic signature validation lives in IStripeWebhookValidator.
        // This validator only guards the basic shape of the inbound HTTP envelope.
        RuleFor(c => c.Json)
            .NotEmpty().WithErrorCode(LocalizationKeys.Validation.RequiredField);

        RuleFor(c => c.Signature)
            .NotEmpty().WithErrorCode(LocalizationKeys.Payment.StripeSignatureInvalid);
    }
}
