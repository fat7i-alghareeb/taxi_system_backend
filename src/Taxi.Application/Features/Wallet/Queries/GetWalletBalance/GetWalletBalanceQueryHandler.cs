using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;
using Taxi.Application.Features.Wallet.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Wallet.Queries.GetWalletBalance;

public class GetWalletBalanceQueryHandler(
    IAppDbContext context,
    IUser currentUser,
    IOptions<WalletOptions> walletOptions)
    : IRequestHandler<GetWalletBalanceQuery, Result<WalletBalanceDto>>
{
    private readonly WalletOptions options = walletOptions.Value;

    public async Task<Result<WalletBalanceDto>> Handle(GetWalletBalanceQuery request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var account = await context.WalletAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

        var currency = account?.Currency
            ?? (string.IsNullOrWhiteSpace(options.Currency) ? "EUR" : options.Currency.Trim().ToUpperInvariant());

        return new WalletBalanceDto(account?.Balance ?? 0m, currency);
    }
}
