using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Application.Features.Wallet.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Wallet.Queries.GetWalletTransactions;

public class GetWalletTransactionsQueryHandler(
    IAppDbContext context,
    IUser currentUser)
    : IRequestHandler<GetWalletTransactionsQuery, Result<PagedResult<WalletTransactionDto>>>
{
    public async Task<Result<PagedResult<WalletTransactionDto>>> Handle(
        GetWalletTransactionsQuery request,
        CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var account = await context.WalletAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

        if (account is null)
        {
            return new PagedResult<WalletTransactionDto>([], 0, page, pageSize);
        }

        var query = context.WalletTransactions
            .AsNoTracking()
            .Where(t => t.WalletAccountId == account.Id);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        return new PagedResult<WalletTransactionDto>(items, totalCount, page, pageSize);
    }
}
