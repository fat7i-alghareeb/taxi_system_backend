using FluentValidation;

namespace Taxi.Application.Features.PaymentPreferences.Commands.SetPaymentPreference;

// Membership against the enabled method types is validated in the handler (needs config);
// this guards the basic shape.
public class SetPaymentPreferenceCommandValidator : AbstractValidator<SetPaymentPreferenceCommand>
{
    public SetPaymentPreferenceCommandValidator()
    {
        RuleFor(v => v.PreferredMethodType)
            .MaximumLength(30)
            .When(v => !string.IsNullOrWhiteSpace(v.PreferredMethodType));
    }
}
