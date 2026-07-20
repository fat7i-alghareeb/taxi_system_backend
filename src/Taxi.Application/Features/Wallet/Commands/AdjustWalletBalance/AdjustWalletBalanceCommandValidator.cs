using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Wallet.Commands.AdjustWalletBalance;

public sealed class AdjustWalletBalanceCommandValidator : AbstractValidator<AdjustWalletBalanceCommand>
{
    public AdjustWalletBalanceCommandValidator()
    {
        RuleFor(v => v.Amount)
            .NotEqual(0m)
            .WithErrorCode(LocalizationKeys.Wallet.InvalidAmount)
            .WithMessage(LocalizationKeys.Wallet.InvalidAmount);

        // Moving someone's money by hand must always carry a why — this is the audit trail.
        RuleFor(v => v.Reason)
            .NotEmpty()
            .WithErrorCode(LocalizationKeys.Wallet.AdjustmentReasonRequired)
            .WithMessage(LocalizationKeys.Wallet.AdjustmentReasonRequired);
    }
}
