using MediatR;
using Taxi.Application.Features.Wallet.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Wallet.Queries.GetWalletBalance;

/// <summary>Returns the current passenger's wallet balance (0 if no account exists yet).</summary>
public record GetWalletBalanceQuery : IRequest<Result<WalletBalanceDto>>;
