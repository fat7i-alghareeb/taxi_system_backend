using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.CustomerIncidents.Commands.ContactPassenger;

public class ContactPassengerCommandValidator : AbstractValidator<ContactPassengerCommand>
{
    public ContactPassengerCommandValidator()
    {
        RuleFor(c => c.IncidentId).NotEmpty();
        RuleFor(c => c.Title)
            .NotEmpty().WithErrorCode(LocalizationKeys.CustomerIncident.ContactTitleRequired)
            .MaximumLength(120);
        RuleFor(c => c.Body)
            .NotEmpty().WithErrorCode(LocalizationKeys.CustomerIncident.ContactBodyRequired)
            .MaximumLength(1000);
    }
}
