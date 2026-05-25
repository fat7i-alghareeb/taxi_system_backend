using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Trips;

namespace Taxi.Infrastructure.Data.Configurations;

public sealed class TripCompensationClaimConfiguration : IEntityTypeConfiguration<TripCompensationClaim>
{
    public void Configure(EntityTypeBuilder<TripCompensationClaim> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.RequestedAmount).HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ReviewNotes).HasMaxLength(2000);
        builder.Property(x => x.EvidenceUrlsJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TripId);
        builder.HasIndex(x => x.Status);
    }
}
