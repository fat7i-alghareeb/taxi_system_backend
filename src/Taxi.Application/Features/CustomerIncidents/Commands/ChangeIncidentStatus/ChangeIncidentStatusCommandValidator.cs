using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.CustomerIncidents.Commands.ChangeIncidentStatus;

public class ChangeIncidentStatusCommandValidator : AbstractValidator<ChangeIncidentStatusCommand>
{
    public ChangeIncidentStatusCommandValidator()
    {
        RuleFor(c => c.IncidentId).NotEmpty();
        RuleFor(c => c.Status)
            .NotEmpty().WithErrorCode(LocalizationKeys.CustomerIncident.InvalidStatus);
    }
}
