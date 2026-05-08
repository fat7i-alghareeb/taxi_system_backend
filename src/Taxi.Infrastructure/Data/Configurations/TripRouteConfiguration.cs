using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Trips;

namespace Taxi.Infrastructure.Data.Configurations;

public class TripRouteConfiguration : IEntityTypeConfiguration<TripRoute>
{
    public void Configure(EntityTypeBuilder<TripRoute> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.TripId)
            .IsUnique();

        builder.Property(r => r.SegmentsJson)
            .HasColumnType("jsonb");
    }
}

