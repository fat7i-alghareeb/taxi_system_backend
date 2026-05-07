using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Vehicles;
using Taxi.Domain.Users;

namespace Taxi.Infrastructure.Data.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Make)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.Model)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.LicensePlate)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(v => v.LicensePlate)
            .IsUnique();

        builder.Property(v => v.Year)
            .HasMaxLength(4)
            .IsRequired();

        builder.Property(v => v.Color)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(v => v.IsActive)
            .HasDefaultValue(true);

        builder.Property(v => v.DeletedAtUtc)
            .IsRequired(false);

        builder.Property(v => v.CreatedAtUtc)
            .IsRequired();

        builder.Property(v => v.LastModifiedUtc)
            .IsRequired(false);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(v => v.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<VehicleType>()
            .WithMany()
            .HasForeignKey(v => v.VehicleTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(v => v.DeletedAtUtc == null);
    }
}
