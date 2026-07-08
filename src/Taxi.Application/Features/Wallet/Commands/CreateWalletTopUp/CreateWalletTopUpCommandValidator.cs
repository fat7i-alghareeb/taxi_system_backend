using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Wallet.Commands.CreateWalletTopUp;

public class CreateWalletTopUpCommandValidator : AbstractValidator<CreateWalletTopUpCommand>
{
    public CreateWalletTopUpCommandValidator()
    {
        // Min/max bounds are configurable and enforced in the handler (parametrized message);
        // this validator guards the basic positive-amount invariant.
        RuleFor(v => v.Amount)
            .GreaterThan(0m)
            .WithErrorCode(LocalizationKeys.Wallet.InvalidAmount)
            .WithMessage(LocalizationKeys.Wallet.InvalidAmount);
    }
}
