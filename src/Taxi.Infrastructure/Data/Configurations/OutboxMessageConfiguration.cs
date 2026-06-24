using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Infrastructure.Outbox;

namespace Taxi.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core mapping for <see cref="OutboxMessage"/> (table "OutboxMessages"). The composite
/// index intentionally mirrors the columns the dispatcher filters and orders by when it
/// claims a batch, so the frequent "find claimable messages" poll stays index-driven and
/// cheap. <see cref="OutboxMessage.Content"/> is stored as PostgreSQL <c>jsonb</c>.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Content).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Error).HasMaxLength(2000);
        builder.HasIndex(x => new
        {
            x.ProcessedAtUtc,
            x.DeadLetteredAtUtc,
            x.NextAttemptAtUtc,
            x.LockedUntilUtc,
            x.OccurredAtUtc,
        });
    }
}
