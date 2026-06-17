using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Users;

namespace Taxi.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

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

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.LastModifiedUtc)
            .IsRequired(false);

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.FcmToken)
            .HasMaxLength(4096)
            .IsRequired(false);

        builder.Property(x => x.PreferredLanguage)
            .HasMaxLength(10)
            .HasDefaultValue("en")
            .IsRequired();

        builder.Property(x => x.StripeCustomerId)
            .HasMaxLength(64)
            .IsRequired(false);

        builder.HasIndex(x => x.StripeCustomerId);

        // Optional home address (owned type). Nullable columns leave existing rows untouched.
        builder.OwnsOne(x => x.HomeAddress, address =>
        {
            address.Property(a => a.Label)
                .HasColumnName("HomeAddressLabel")
                .HasMaxLength(500);

            address.Property(a => a.Latitude)
                .HasColumnName("HomeAddressLatitude")
                .HasPrecision(18, 10);

            address.Property(a => a.Longitude)
                .HasColumnName("HomeAddressLongitude")
                .HasPrecision(18, 10);
        });

        // Global query filter for soft delete
        builder.HasQueryFilter(x => x.DeletedAtUtc == null);
    }
}

