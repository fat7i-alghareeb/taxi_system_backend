using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Trips;

namespace Taxi.Infrastructure.Data.Configurations;

public sealed class PendingTripEditConfiguration : IEntityTypeConfiguration<PendingTripEdit>
{
    public void Configure(EntityTypeBuilder<PendingTripEdit> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ProposedStopsJson);
        builder.Property(x => x.DeltaAmount).HasPrecision(18, 2);
        builder.Property(x => x.WalletDebitedAmount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.StripePaymentIntentId).HasMaxLength(255).IsRequired();
        builder.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TripId);
        builder.HasIndex(x => x.StripePaymentIntentId);
        builder.HasIndex(x => x.Status);
    }
}
