using MediatR;
using Taxi.Application.Features.Wallet.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Wallet.Commands.CreateWalletTopUp;

/// <summary>Starts a Stripe-funded top-up of the current passenger's wallet balance.</summary>
public record CreateWalletTopUpCommand(decimal Amount) : IRequest<Result<WalletTopUpDto>>;
