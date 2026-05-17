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

        builder.Property(q => q.OriginalFare)
            .HasPrecision(10, 2);

        builder.Property(q => q.DiscountPercent)
            .HasPrecision(5, 2);

        builder.Property(q => q.ValidUntil)
            .IsRequired();

        builder.Property(q => q.Used)
            .HasDefaultValue(false);

        builder.Property(q => q.CreatedAtUtc)
            .IsRequired();

        builder.Property(q => q.PassengerId)
            .IsRequired();

        builder.Property(q => q.VehicleTypeId)
            .IsRequired();

        builder.OwnsMany(q => q.Stops, stops =>
        {
            stops.ToJson();
            stops.Property(c => c.Latitude).HasPrecision(18, 10);
            stops.Property(c => c.Longitude).HasPrecision(18, 10);
        });
    }
}

