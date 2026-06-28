using FluentValidation;

namespace Taxi.Application.Features.CustomerIncidents.Commands.RefundIncident;

public class RefundIncidentCommandValidator : AbstractValidator<RefundIncidentCommand>
{
    public RefundIncidentCommandValidator()
    {
        RuleFor(c => c.IncidentId).NotEmpty();

        // A null amount means "refund the full captured amount"; when supplied it
        // must be a positive value.
        RuleFor(c => c.Amount)
            .GreaterThan(0)
            .When(c => c.Amount.HasValue);
    }
}
