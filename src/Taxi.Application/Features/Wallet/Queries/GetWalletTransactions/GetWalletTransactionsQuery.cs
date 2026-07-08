using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Application.Features.Wallet.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Wallet.Queries.GetWalletTransactions;

/// <summary>Returns the current passenger's wallet ledger, newest first, paginated.</summary>
public record GetWalletTransactionsQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<WalletTransactionDto>>>;
