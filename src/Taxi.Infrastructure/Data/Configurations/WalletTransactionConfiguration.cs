using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Wallet;

namespace Taxi.Infrastructure.Data.Configurations;

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.WalletAccountId)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Direction)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Property(x => x.BalanceAfter)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.StripePaymentIntentId)
            .HasMaxLength(255);

        builder.Property(x => x.StripeChargeId)
            .HasMaxLength(255);

        builder.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.LastModifiedUtc)
            .IsRequired(false);

        // Exactly-once posting guard: a retried operation reuses the same key and hits this
        // unique index instead of double-crediting.
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique();

        builder.HasIndex(x => x.WalletAccountId);
        builder.HasIndex(x => x.Status);

        // The success webhook resolves the pending top-up by its Stripe PaymentIntent id.
        builder.HasIndex(x => x.StripePaymentIntentId)
            .HasFilter("\"StripePaymentIntentId\" IS NOT NULL");

        builder.HasOne<WalletAccount>()
            .WithMany()
            .HasForeignKey(x => x.WalletAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
