using MediatR;

using Taxi.Application.Features.Wallet.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Wallet.Commands.AdjustWalletBalance;

/// <summary>
/// An admin's manual correction to a customer's balance. Positive <paramref name="Amount"/>
/// credits — the usual case, waiving a debt the customer should not carry; negative debits.
/// </summary>
public sealed record AdjustWalletBalanceCommand(
    Guid UserId,
    decimal Amount,
    string Reason) : IRequest<Result<WalletBalanceDto>>;
