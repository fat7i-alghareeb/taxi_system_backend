using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Trips;

namespace Taxi.Infrastructure.Data.Configurations;

public class PricingQuoteConfiguration : IEntityTypeConfiguration<PricingQuote>
{
    public void Configure(EntityTypeBuilder<PricingQuote> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(q => q.TotalDistanceKm)
            .HasPrecision(10, 3);

        builder.Property(q => q.TotalDurationMin)
            .HasPrecision(10, 2);

        builder.Property(q => q.FinalFare)
            .HasPrecision(10, 2);
    }
}
