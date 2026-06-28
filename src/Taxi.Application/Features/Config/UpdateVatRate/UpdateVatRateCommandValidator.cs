using FluentValidation;

namespace Taxi.Application.Features.Config.UpdateVatRate;

public class UpdateVatRateCommandValidator : AbstractValidator<UpdateVatRateCommand>
{
    public UpdateVatRateCommandValidator()
    {
        // Stored as a fraction; the invoice domain requires 0 <= rate < 1.
        RuleFor(x => x.Rate)
            .GreaterThanOrEqualTo(0)
            .LessThan(1);
    }
}
