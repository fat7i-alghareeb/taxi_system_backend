using FluentValidation;

namespace Taxi.Application.Features.Config.UpdateSupportContact;

public class UpdateSupportContactCommandValidator : AbstractValidator<UpdateSupportContactCommand>
{
    public UpdateSupportContactCommandValidator()
    {
        // The number is required — a blank value would delete the row and break
        // the customer app's in-trip "Report problem" action.
        // Accept digits with an optional leading '+' (E.164-style, no spaces/dashes),
        // which is what wa.me expects.
        RuleFor(x => x.WhatsApp)
            .NotEmpty()
            .WithErrorCode("SupportContact.WhatsAppRequired")
            .MaximumLength(20)
            .WithErrorCode("SupportContact.WhatsAppInvalid")
            .Matches(@"^\+?[0-9]{6,15}$")
            .WithErrorCode("SupportContact.WhatsAppInvalid")
            .When(x => !string.IsNullOrWhiteSpace(x.WhatsApp));
    }
}
