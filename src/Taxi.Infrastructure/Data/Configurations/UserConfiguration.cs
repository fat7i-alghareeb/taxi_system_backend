using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Users;

namespace Taxi.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);

        // Trilingual name stored as a single JSONB column: {"En":"...", "Ar":"...", "Nl":"..."}
        builder.OwnsOne(x => x.Name, name =>
        {
            name.ToJson();
        });

        builder.Property(x => x.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(x => x.Phone)
            .IsUnique();

        builder.Property(x => x.Email)
            .HasMaxLength(150);

        builder.Property(x => x.ProfilePhotoUrl)
            .HasMaxLength(500);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.Property(x => x.ActiveVehicleId)
            .IsRequired(false);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.LastModifiedUtc)
            .IsRequired(false);

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Global query filter for soft delete
        builder.HasQueryFilter(x => x.DeletedAtUtc == null);
    }
}

