using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Taxi.Api.IntegrationTests.Infrastructure;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Users;
using Taxi.Domain.Wallet;
using Taxi.Infrastructure.Data;

using Xunit;

namespace Taxi.Api.IntegrationTests.Wallet;

/// <summary>
/// Exercises the wallet debit / hold / reversal paths against a real Postgres container.
/// Complements <see cref="WalletTopUpIntegrationTests"/> (which covers top-up crediting):
/// here we prove there is no double-deduction or overdraft under concurrency, that debit
/// and reversal are idempotent, that holds commit/release correctly, and that the materialized
/// balance always reconciles to the committed ledger.
/// </summary>
[Collection("ApiTestCollection")]
public class WalletLedgerIntegrityTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task ConcurrentDebits_NeverOverdraw_AndDebitExactlyTheAvailableBalance()
    {
        var userId = await SeedFundedWalletAsync(startingBalance: 100m);
        var tripId = Guid.NewGuid();

        // Five concurrent fee debits of 40 each want 200 total, but only 100 is available.
        // Each uses a distinct idempotency key, so none is an idempotent no-op — the design
        // must serialize them (optimistic token + retry) and hand out exactly 100 in total.
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            await using var scope = fixture.Factory.Services.CreateAsyncScope();
            var wallet = scope.ServiceProvider.GetRequiredService<IWalletService>();
            return await wallet.DebitForFeeAsync(
                userId, tripId, 40m, "EUR", $"fee {i}", $"fee-{tripId:N}-{i}");
        });

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.IsSuccess));
        var totalDebited = results.Sum(r => r.Value.DebitedAmount);
        Assert.Equal(100m, totalDebited);

        var balance = await GetBalanceAsync(userId);
        Assert.Equal(0m, balance);
        Assert.True(balance >= 0m, "Wallet balance must never go negative.");

        await AssertReconcilesAsync(userId);
    }

    [Fact]
    public async Task DebitForFee_IsIdempotent_OnRetryWithSameKey()
    {
        var userId = await SeedFundedWalletAsync(startingBalance: 50m);
        var tripId = Guid.NewGuid();
        var key = $"fee-{tripId:N}";

        WalletDebitOutcome first;
        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var wallet = scope.ServiceProvider.GetRequiredService<IWalletService>();
            var r = await wallet.DebitForFeeAsync(userId, tripId, 20m, "EUR", "fee", key);
            Assert.True(r.IsSuccess);
            first = r.Value;
        }
        Assert.Equal(20m, first.DebitedAmount);

        // Same key again (API retry): must not debit twice.
        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var wallet = scope.ServiceProvider.GetRequiredService<IWalletService>();
            var r = await wallet.DebitForFeeAsync(userId, tripId, 20m, "EUR", "fee", key);
            Assert.True(r.IsSuccess);
            Assert.Equal(20m, r.Value.DebitedAmount);
        }

        Assert.Equal(30m, await GetBalanceAsync(userId));
        await AssertReconcilesAsync(userId);
    }

    [Fact]
    public async Task RefundReversal_IsIdempotent_AndCreditsOnce()
    {
        var userId = await SeedFundedWalletAsync(startingBalance: 40m);
        var tripId = Guid.NewGuid();
        var key = $"refund-reversal-{Guid.NewGuid():N}";

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await using var scope = fixture.Factory.Services.CreateAsyncScope();
            var wallet = scope.ServiceProvider.GetRequiredService<IWalletService>();
            var r = await wallet.CreditRefundReversalAsync(
                userId, tripId, Guid.NewGuid(), Guid.NewGuid(), 15m, "EUR", key);
            Assert.True(r.IsSuccess);
        }

        // Credited exactly once despite three deliveries.
        Assert.Equal(55m, await GetBalanceAsync(userId));
        await AssertReconcilesAsync(userId);
    }

    [Fact]
    public async Task TripHold_Commit_ReducesBalancePermanently()
    {
        var userId = await SeedFundedWalletAsync(startingBalance: 60m);
        var tripId = Guid.NewGuid();

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var wallet = scope.ServiceProvider.GetRequiredService<IWalletService>();
            var hold = await wallet.TryHoldForTripAsync(userId, tripId, 25m, "EUR", $"hold-{tripId:N}");
            Assert.True(hold.IsSuccess);
            Assert.Equal(25m, hold.Value.HeldAmount);
        }

        // The hold reserves immediately, so the balance is already reduced.
        Assert.Equal(35m, await GetBalanceAsync(userId));

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var wallet = scope.ServiceProvider.GetRequiredService<IWalletService>();
            var committed = await wallet.CommitTripHoldAsync(tripId);
            Assert.True(committed.IsSuccess);
            Assert.Equal(25m, committed.Value);
        }

        Assert.Equal(35m, await GetBalanceAsync(userId));
        await AssertReconcilesAsync(userId);
    }

    [Fact]
    public async Task TripHold_Release_RestoresBalance()
    {
        var userId = await SeedFundedWalletAsync(startingBalance: 60m);
        var tripId = Guid.NewGuid();

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var wallet = scope.ServiceProvider.GetRequiredService<IWalletService>();
            var hold = await wallet.TryHoldForTripAsync(userId, tripId, 25m, "EUR", $"hold-{tripId:N}");
            Assert.True(hold.IsSuccess);
        }
        Assert.Equal(35m, await GetBalanceAsync(userId));

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var wallet = scope.ServiceProvider.GetRequiredService<IWalletService>();
            var released = await wallet.ReleaseTripHoldAsync(tripId);
            Assert.True(released.IsSuccess);
        }

        // The released hold restores the full balance and leaves no committed debit behind.
        Assert.Equal(60m, await GetBalanceAsync(userId));
        await AssertReconcilesAsync(userId);
    }

    // The core financial invariant: the materialized account balance must always equal the
    // sum of committed credits minus committed debits. Pending holds reduce the balance too,
    // so we only reconcile once holds are terminal (committed or released).
    private async Task AssertReconcilesAsync(Guid userId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var account = await context.WalletAccounts.AsNoTracking().FirstAsync(a => a.UserId == userId);

        var committed = await context.WalletTransactions
            .AsNoTracking()
            .Where(t => t.WalletAccountId == account.Id && t.Status == WalletTransactionStatus.Committed)
            .ToListAsync();

        var pendingHolds = await context.WalletTransactions
            .AsNoTracking()
            .Where(t => t.WalletAccountId == account.Id && t.Status == WalletTransactionStatus.Pending)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        var creditSum = committed
            .Where(t => t.Direction == WalletTransactionDirection.Credit)
            .Sum(t => t.Amount);
        var debitSum = committed
            .Where(t => t.Direction == WalletTransactionDirection.Debit)
            .Sum(t => t.Amount);

        // Pending holds are debits that already reduced the balance but are not yet committed.
        Assert.Equal(creditSum - debitSum - pendingHolds, account.Balance);
    }

    private async Task<decimal> GetBalanceAsync(Guid userId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await context.WalletAccounts.AsNoTracking().FirstAsync(a => a.UserId == userId);
        return account.Balance;
    }

    private async Task<Guid> SeedFundedWalletAsync(decimal startingBalance)
    {
        var userId = await SeedPassengerAsync();
        var paymentIntentId = $"pi_seed_{Guid.NewGuid():N}";

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var wallet = scope.ServiceProvider.GetRequiredService<IWalletService>();

        var pending = await wallet.CreatePendingTopUpAsync(
            Guid.NewGuid(), userId, startingBalance, "EUR", paymentIntentId);
        Assert.True(pending.IsSuccess);

        var credit = await wallet.CreditTopUpFromWebhookAsync(paymentIntentId, startingBalance, "ch_seed");
        Assert.True(credit.IsSuccess);
        Assert.Equal(WalletTopUpCreditStatus.Credited, credit.Value.Status);

        return userId;
    }

    private async Task<Guid> SeedPassengerAsync()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var phone = "+319" + Random.Shared.Next(10_000_000, 99_999_999).ToString();
        var user = User.Create(Guid.NewGuid(), "Ledger Tester", phone, null, UserRole.Passenger).Value;

        context.DomainUsers.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }
}
