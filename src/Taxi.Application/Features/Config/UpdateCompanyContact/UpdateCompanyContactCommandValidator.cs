using FluentValidation;

namespace Taxi.Application.Features.Config.UpdateCompanyContact;

public class UpdateCompanyContactCommandValidator : AbstractValidator<UpdateCompanyContactCommand>
{
    public UpdateCompanyContactCommandValidator()
    {
        // All fields are optional (a blank clears the value). Validate format only
        // when something was provided.
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithErrorCode("CompanyContact.EmailInvalid")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(30)
            .WithErrorCode("CompanyContact.PhoneInvalid")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Website)
            .MaximumLength(200)
            .WithErrorCode("CompanyContact.WebsiteInvalid")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));
    }
}
