using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Taxi.Api.IntegrationTests.Infrastructure;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Users;
using Taxi.Domain.Wallet;
using Taxi.Infrastructure.Data;

using Xunit;

namespace Taxi.Api.IntegrationTests.Wallet;

// Exercises the wallet ledger against a real Postgres container: crediting happens exactly
// once even when the payment_intent.succeeded webhook is delivered multiple times
// (sequentially and concurrently), proving the idempotency + optimistic-concurrency design.
[Collection("ApiTestCollection")]
public class WalletTopUpIntegrationTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task TopUp_CreditsOnce_AndDuplicateWebhookDoesNotDoubleCredit()
    {
        var userId = await SeedPassengerAsync();
        var txnId = Guid.NewGuid();
        var paymentIntentId = $"pi_int_{Guid.NewGuid():N}";

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var wallet = scope.ServiceProvider.GetRequiredService<IWalletService>();

            var pending = await wallet.CreatePendingTopUpAsync(txnId, userId, 25m, "EUR", paymentIntentId);
            Assert.True(pending.IsSuccess);

            var firstCredit = await wallet.CreditTopUpFromWebhookAsync(paymentIntentId, 25m, "ch_1");
            Assert.True(firstCredit.IsSuccess);
            Assert.Equal(WalletTopUpCreditStatus.Credited, firstCredit.Value.Status);
            Assert.Equal(25m, firstCredit.Value.NewBalance);
        }

        // Duplicate webhook delivery in a fresh scope: must be an idempotent no-op.
        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var wallet = scope.ServiceProvider.GetRequiredService<IWalletService>();
            var secondCredit = await wallet.CreditTopUpFromWebhookAsync(paymentIntentId, 25m, "ch_1");
            Assert.True(secondCredit.IsSuccess);
            Assert.Equal(WalletTopUpCreditStatus.AlreadyCredited, secondCredit.Value.Status);
        }

        await AssertBalanceAndCommittedCountAsync(userId, expectedBalance: 25m, expectedCommitted: 1);
    }

    [Fact]
    public async Task ConcurrentDuplicateWebhooks_CreditExactlyOnce()
    {
        var userId = await SeedPassengerAsync();
        var txnId = Guid.NewGuid();
        var paymentIntentId = $"pi_int_{Guid.NewGuid():N}";

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var wallet = scope.ServiceProvider.GetRequiredService<IWalletService>();
            var pending = await wallet.CreatePendingTopUpAsync(txnId, userId, 30m, "EUR", paymentIntentId);
            Assert.True(pending.IsSuccess);
        }

        // Five concurrent deliveries of the same succeeded webhook, each in its own scope.
        var tasks = Enumerable.Range(0, 5).Select(async _ =>
        {
            await using var scope = fixture.Factory.Services.CreateAsyncScope();
            var wallet = scope.ServiceProvider.GetRequiredService<IWalletService>();
            return await wallet.CreditTopUpFromWebhookAsync(paymentIntentId, 30m, "ch_x");
        });

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.IsSuccess));
        Assert.Equal(1, results.Count(r => r.Value.Status == WalletTopUpCreditStatus.Credited));

        await AssertBalanceAndCommittedCountAsync(userId, expectedBalance: 30m, expectedCommitted: 1);
    }

    private async Task AssertBalanceAndCommittedCountAsync(Guid userId, decimal expectedBalance, int expectedCommitted)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var account = await context.WalletAccounts.AsNoTracking().FirstAsync(a => a.UserId == userId);
        Assert.Equal(expectedBalance, account.Balance);

        var committed = await context.WalletTransactions
            .AsNoTracking()
            .CountAsync(t => t.WalletAccountId == account.Id && t.Status == WalletTransactionStatus.Committed);
        Assert.Equal(expectedCommitted, committed);
    }

    private async Task<Guid> SeedPassengerAsync()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var phone = "+319" + Random.Shared.Next(10_000_000, 99_999_999).ToString();
        var user = User.Create(Guid.NewGuid(), "Wallet Tester", phone, null, UserRole.Passenger).Value;

        context.DomainUsers.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }
}
