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
    }
}
