using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Invoices;
using Taxi.Infrastructure.Data;

namespace Taxi.Infrastructure.Services.Invoices;

/// <summary>
/// Allocates the next per-month sequence atomically using a single-row
/// counter (<see cref="InvoiceCounter"/>) per <c>YYYYMM</c> bucket and a
/// serializable transaction with row-level locking on PostgreSQL.
/// </summary>
/// <remarks>
/// The DbContext is configured with <c>EnableRetryOnFailure</c>, so the
/// transactional block runs inside an execution strategy — otherwise EF
/// throws <c>InvalidOperationException</c> on any user-initiated transaction.
/// </remarks>
public sealed class SequentialInvoiceNumberGenerator(AppDbContext db) : IInvoiceNumberGenerator
{
    private const string Prefix = "OT";

    public async Task<string> NextAsync(DateTimeOffset issuedAtUtc, CancellationToken ct)
    {
        var bucket = issuedAtUtc.ToUniversalTime().ToString("yyyyMM");

        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(
            state: (db, bucket),
            operation: static async (ctx, s, token) =>
            {
                var (db, bucket) = s;

                await using var tx = await db.Database
                    .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, token);

                // SELECT ... FOR UPDATE on the bucket row (PostgreSQL row-level lock).
                var counter = await db.InvoiceCounters
                    .FromSqlInterpolated($"SELECT * FROM \"InvoiceCounters\" WHERE \"YearMonth\" = {bucket} FOR UPDATE")
                    .FirstOrDefaultAsync(token);

                int allocated;
                if (counter is null)
                {
                    counter = new InvoiceCounter(bucket, nextSequence: 2);
                    db.InvoiceCounters.Add(counter);
                    allocated = 1;
                }
                else
                {
                    allocated = counter.Allocate();
                }

                await db.SaveChangesAsync(token);
                await tx.CommitAsync(token);

                return $"{Prefix}-{bucket}-{allocated:D6}";
            },
            verifySucceeded: null,
            cancellationToken: ct);
    }
}
