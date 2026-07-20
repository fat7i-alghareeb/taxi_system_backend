using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Wallet;

namespace Taxi.Application.Features.Wallet.Common;

/// <summary>
/// Refuses a customer who owes money from starting a new booking. A fee that could not be
/// collected leaves the wallet balance negative; until it is settled the customer must not be
/// able to take another ride, or the debt simply compounds.
/// </summary>
/// <remarks>
/// Applied at BOTH quoting and booking. Quoting is the earlier and friendlier place to say no —
/// it also avoids a Google Directions call and the quote rows that call persists — but booking is
/// the authoritative gate, since quoting can be skipped and a quote can be replayed.
/// </remarks>
public interface IWalletDebtGuard
{
    /// <summary>Returns the blocking error when the customer owes money, otherwise null.</summary>
    Task<Error?> CheckAsync(Guid userId, CancellationToken ct = default);
}

public sealed class WalletDebtGuard(
    IAppDbContext context,
    IOptions<WalletOptions> walletOptions) : IWalletDebtGuard
{
    private readonly WalletOptions options = walletOptions.Value;

    public async Task<Error?> CheckAsync(Guid userId, CancellationToken ct = default)
    {
        var account = await context.WalletAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

        if (account is null || !account.IsInDebt)
        {
            return null;
        }

        // Below the threshold the debt is real but unchargeable through Stripe, so blocking would
        // strand the customer with no way out. Carry it silently instead.
        if (account.AmountOwed < options.MinDebtToBlock)
        {
            return null;
        }

        return WalletErrors.OutstandingDebt(account.AmountOwed, account.Currency);
    }
}
