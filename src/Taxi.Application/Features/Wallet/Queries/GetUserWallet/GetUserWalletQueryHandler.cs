using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Wallet.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Wallet.Queries.GetUserWallet;

public class GetUserWalletQueryHandler(IAppDbContext context)
    : IRequestHandler<GetUserWalletQuery, Result<AdminUserWalletDto>>
{
    public async Task<Result<AdminUserWalletDto>> Handle(GetUserWalletQuery request, CancellationToken ct)
    {
        var limit = request.TransactionLimit is < 1 or > 200 ? 50 : request.TransactionLimit;

        var account = await context.WalletAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == request.UserId, ct);

        if (account is null)
        {
            return new AdminUserWalletDto(request.UserId, HasAccount: false, Balance: 0m, "EUR", []);
        }

        var transactions = await context.WalletTransactions
            .AsNoTracking()
            .Where(t => t.WalletAccountId == account.Id)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(limit)
            .Select(t => new WalletTransactionDto(
                t.Id,
                t.Type.ToString(),
                t.Direction.ToString(),
                t.Amount,
                t.Currency,
                t.BalanceAfter,
                t.Status.ToString(),
                t.Description,
                t.CreatedAtUtc,
                t.CompletedAtUtc))
            .ToListAsync(ct);

        return new AdminUserWalletDto(
            request.UserId,
            HasAccount: true,
            account.Balance,
            account.Currency,
            transactions,
            account.AmountOwed);
    }
}
