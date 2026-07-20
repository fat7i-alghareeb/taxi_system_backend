using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;
using Taxi.Application.Features.Wallet.Dtos;
using Taxi.Contracts.Common;
using Taxi.Contracts.Notifications;
using Taxi.Domain.Audit;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Wallet.Commands.AdjustWalletBalance;

/// <summary>
/// Lets an admin waive a debt or correct a balance by hand. Needed because a debt blocks the
/// customer from booking: without an override, a disputed or mis-charged fee would strand them
/// until someone edited the database directly.
/// </summary>
public sealed class AdjustWalletBalanceCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IWalletService wallet,
    ITripNotifier tripNotifier,
    IOptions<WalletOptions> walletOptions,
    ILogger<AdjustWalletBalanceCommandHandler> logger)
    : IRequestHandler<AdjustWalletBalanceCommand, Result<WalletBalanceDto>>
{
    private readonly WalletOptions options = walletOptions.Value;

    public async Task<Result<WalletBalanceDto>> Handle(AdjustWalletBalanceCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var adminId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var user = await context.DomainUsers.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
        {
            return Error.NotFound(LocalizationKeys.User.NotFound, "User not found.");
        }

        var currency = string.IsNullOrWhiteSpace(options.Currency)
            ? "EUR"
            : options.Currency.Trim().ToUpperInvariant();
        var reason = request.Reason.Trim();

        // Keyed on the admin + user + amount + reason so a double-submitted form is a no-op
        // rather than a second adjustment.
        var idempotencyKey = $"wallet-adjust-{request.UserId:N}-{adminId:N}-{request.Amount}-{reason.GetHashCode():X}";

        var result = await wallet.AdjustBalanceAsync(
            request.UserId, request.Amount, currency, reason, adminId, idempotencyKey, ct);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var audit = AuditLog.Create(
            Guid.NewGuid(),
            adminId,
            action: "AdjustedWalletBalance",
            entityName: "WalletAccount",
            entityId: request.UserId.ToString(),
            oldValue: null,
            newValue: $"{request.Amount:0.00} {currency}: {reason}");
        if (audit.IsSuccess)
        {
            context.AuditLogs.Add(audit.Value);
            await context.SaveChangesAsync(ct);
        }

        var balance = result.Value;
        var amountOwed = balance < 0m ? -balance : 0m;

        await NotifyBalanceChangedAsync(request.UserId, balance, currency, ct);
        return new WalletBalanceDto(
            balance,
            currency,
            amountOwed,
            IsBookingBlocked: amountOwed >= options.MinDebtToBlock);
    }

    private async Task NotifyBalanceChangedAsync(Guid userId, decimal balance, string currency, CancellationToken ct)
    {
        try
        {
            await tripNotifier.NotifyWalletBalanceChangedAsync(
                new WalletBalanceChangedNotification(
                    userId,
                    balance,
                    balance < 0m ? -balance : 0m,
                    currency,
                    Guid.NewGuid()),
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast adjusted wallet balance for user {UserId}.", userId);
        }
    }
}
