using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Vehicles;

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

        builder.HasOne<VehicleType>()
            .WithMany()
            .HasForeignKey(v => v.VehicleTypeId);
    }
}
