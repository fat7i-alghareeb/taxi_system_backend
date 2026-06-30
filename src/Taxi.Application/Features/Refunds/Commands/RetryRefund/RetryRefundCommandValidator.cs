using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Refunds.Commands.RetryRefund;

public sealed class RetryRefundCommandValidator : AbstractValidator<RetryRefundCommand>
{
    public RetryRefundCommandValidator()
    {
        RuleFor(x => x.RefundId)
            .NotEmpty()
            .WithMessage(LocalizationKeys.Payment.NotFound);
    }
}
