using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Users;
using Taxi.Domain.Wallet;

namespace Taxi.Infrastructure.Data.Configurations;

public class WalletAccountConfiguration : IEntityTypeConfiguration<WalletAccount>
{
    public void Configure(EntityTypeBuilder<WalletAccount> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        // Optimistic concurrency guard on the money field itself: EF includes the original
        // Balance in the UPDATE WHERE clause, so two concurrent credits can't lose a write
        // (the loser gets DbUpdateConcurrencyException and retries against the fresh balance).
        // Every wallet money movement changes Balance, so this token covers all of them.
        builder.Property(x => x.Balance)
            .HasPrecision(18, 2)
            .IsConcurrencyToken();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.LastModifiedUtc)
            .IsRequired(false);

        // One wallet per user.
        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
