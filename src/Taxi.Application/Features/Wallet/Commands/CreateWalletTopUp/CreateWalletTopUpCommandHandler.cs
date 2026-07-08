using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Application.Features.Wallet.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Wallet;

namespace Taxi.Application.Features.Wallet.Commands.CreateWalletTopUp;

public class CreateWalletTopUpCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IClientConfigProvider clientConfig,
    IStripePaymentService stripe,
    IWalletService wallet,
    IOptions<WalletOptions> walletOptions)
    : IRequestHandler<CreateWalletTopUpCommand, Result<WalletTopUpDto>>
{
    private readonly WalletOptions options = walletOptions.Value;

    public async Task<Result<WalletTopUpDto>> Handle(CreateWalletTopUpCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        // Stripe is the only funding source for a wallet top-up.
        if (!clientConfig.GetClientConfig().StripeEnabled)
        {
            return WalletErrors.TopUpDisabled;
        }

        var currency = string.IsNullOrWhiteSpace(options.Currency)
            ? "EUR"
            : options.Currency.Trim().ToUpperInvariant();
        var amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero);

        if (amount < options.MinTopUpAmount)
        {
            return WalletErrors.TopUpBelowMinimum(options.MinTopUpAmount, currency);
        }

        if (amount > options.MaxTopUpAmount)
        {
            return WalletErrors.TopUpAboveMaximum(options.MaxTopUpAmount, currency);
        }

        var user = await context.DomainUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Error.NotFound(LocalizationKeys.Auth.UserNotFound, "User not found.");
        }

        // One ledger id up front: it seeds the Stripe idempotency key and links the
        // PaymentIntent metadata to the pending WalletTransaction the webhook will commit.
        var walletTransactionId = Guid.NewGuid();

        var intentResult = await stripe.CreateTopUpPaymentIntentAsync(
            walletTransactionId: walletTransactionId,
            amount: amount,
            currency: currency,
            userId: userId,
            existingStripeCustomerId: user.StripeCustomerId,
            userEmail: user.Email,
            userPhone: user.Phone,
            userName: user.Name,
            ct: ct);

        if (intentResult.IsFailure)
        {
            return intentResult.Error;
        }

        var intent = intentResult.Value;

        if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
        {
            user.SetStripeCustomerId(intent.CustomerId);
        }

        // Persists the pending top-up ledger entry (and the user's new StripeCustomerId, if set,
        // since both share the request-scoped DbContext) in one SaveChanges.
        var pendingResult = await wallet.CreatePendingTopUpAsync(
            walletTransactionId,
            userId,
            amount,
            currency,
            intent.PaymentIntentId,
            ct);

        if (pendingResult.IsFailure)
        {
            return pendingResult.Error;
        }

        var stripePayment = new StripePaymentDto(
            intent.PaymentIntentId,
            intent.ClientSecret,
            intent.PublishableKey,
            intent.CustomerId,
            intent.EphemeralKeySecret);

        return new WalletTopUpDto(amount, currency, stripePayment);
    }
}
