using FluentValidation;

namespace Taxi.Application.Features.Trips.Commands.SettleWaitingFee;

public class SettleWaitingFeeCommandValidator : AbstractValidator<SettleWaitingFeeCommand>
{
    public SettleWaitingFeeCommandValidator()
    {
        RuleFor(v => v.TripId).NotEmpty();
    }
}
