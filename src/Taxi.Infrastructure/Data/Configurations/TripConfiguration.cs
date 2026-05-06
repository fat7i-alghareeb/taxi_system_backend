using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Trips;
using Taxi.Domain.Users;
using Taxi.Domain.Vehicles;

namespace Taxi.Infrastructure.Data.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<VehicleType>()
            .WithMany()
            .HasForeignKey(t => t.VehicleTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PricingQuote>()
            .WithMany()
            .HasForeignKey(t => t.QuoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.OwnsMany(t => t.Stops, s =>
        {
            s.ToTable("TripStops");
            s.HasKey("Id");
            s.Property<Guid>("Id").ValueGeneratedOnAdd();
            s.OwnsOne(x => x.Coordinate, c =>
            {
                c.Property(p => p.Latitude).HasPrecision(18, 10);
                c.Property(p => p.Longitude).HasPrecision(18, 10);
            });
        });

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired();
    }
}
