using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Trips;

namespace Taxi.Infrastructure.Data.Configurations;

public sealed class TripCancellationConfiguration : IEntityTypeConfiguration<TripCancellation>
{
    public void Configure(EntityTypeBuilder<TripCancellation> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Actor).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Reason).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.RefundPercent).HasPrecision(5, 2);
        builder.Property(x => x.RefundAmount).HasPrecision(18, 2);
        builder.Property(x => x.CancellationFeeAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TripId);
    }
}
