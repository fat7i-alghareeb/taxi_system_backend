using MediatR;
using Taxi.Application.Features.Wallet.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Wallet.Queries.GetUserWallet;

/// <summary>Admin: a passenger's wallet balance + recent ledger (read-only).</summary>
public record GetUserWalletQuery(Guid UserId, int TransactionLimit = 50)
    : IRequest<Result<AdminUserWalletDto>>;
