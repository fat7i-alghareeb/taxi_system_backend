using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Vehicles;

namespace Taxi.Infrastructure.Data.Configurations;

public class VehicleTypeConfiguration : IEntityTypeConfiguration<VehicleType>
{
    public void Configure(EntityTypeBuilder<VehicleType> builder)
    {
        builder.HasKey(t => t.Id);

        builder.OwnsOne(t => t.Name, n =>
        {
            n.ToJson();
        });

        builder.Property(t => t.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(t => t.Code)
            .IsUnique();

        builder.Property(t => t.CurrencyCode)
            .HasMaxLength(3)
            .HasDefaultValue("EUR");
    }
}
