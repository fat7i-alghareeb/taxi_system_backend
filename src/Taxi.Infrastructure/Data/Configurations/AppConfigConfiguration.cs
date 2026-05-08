using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Configuration;

namespace Taxi.Infrastructure.Data.Configurations;

public class AppConfigConfiguration : IEntityTypeConfiguration<AppConfig>
{
    public void Configure(EntityTypeBuilder<AppConfig> builder)
    {
        builder.HasKey(c => c.Key);

        builder.Property(c => c.Key)
            .HasMaxLength(100);

        builder.Property(c => c.Value)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.UpdatedAtUtc)
            .IsRequired();
    }
}

