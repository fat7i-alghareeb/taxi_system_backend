using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Drivers;
using Taxi.Domain.Users;
using Taxi.Domain.Vehicles;

namespace Taxi.Infrastructure.Data.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.HasKey(d => d.Id);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .IsRequired();

        builder.HasOne<Vehicle>()
            .WithMany()
            .HasForeignKey(d => d.ActiveVehicleId);

        builder.Property(d => d.LicenseNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(d => d.CurrentLat)
            .HasPrecision(18, 10);

        builder.Property(d => d.CurrentLng)
            .HasPrecision(18, 10);

        builder.Property(d => d.AcceptanceRate)
            .HasPrecision(5, 2)
            .HasDefaultValue(1.0);

        builder.Property(d => d.CompletionRate)
            .HasPrecision(5, 2)
            .HasDefaultValue(1.0);

        builder.Property(d => d.TotalTripsCompleted)
            .HasDefaultValue(0);

        builder.Property(d => d.IsActive)
            .HasDefaultValue(true);

        builder.Property(d => d.DeletedAtUtc)
            .IsRequired(false);

        builder.Property(d => d.CreatedAtUtc)
            .IsRequired();

        builder.Property(d => d.LastModifiedUtc)
            .IsRequired(false);

        builder.HasQueryFilter(d => d.DeletedAtUtc == null);
    }
}

