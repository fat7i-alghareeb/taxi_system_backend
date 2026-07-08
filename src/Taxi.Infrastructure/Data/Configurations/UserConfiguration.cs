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

        // Only VERIFIED phones are unique. Unverified phones are contact/profile data and
        // may duplicate; they can never be used for phone login. A partial index keeps the
        // real owner from being blocked by someone else's unverified copy of the number.
        builder.HasIndex(x => x.Phone)
            .IsUnique()
            .HasFilter("\"IsPhoneVerified\" AND \"DeletedAtUtc\" IS NULL");

        builder.Property(x => x.Email)
            .HasMaxLength(150);

        // Only VERIFIED emails are unique and usable as a login identity. Unverified emails
        // are contact data.
        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasFilter("\"IsEmailVerified\" AND \"Email\" IS NOT NULL AND \"DeletedAtUtc\" IS NULL");

        builder.Property(x => x.IsPhoneVerified)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsEmailVerified)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.GoogleId)
            .HasMaxLength(128);

        // A Google identity maps to at most one active account.
        builder.HasIndex(x => x.GoogleId)
            .IsUnique()
            .HasFilter("\"GoogleId\" IS NOT NULL AND \"DeletedAtUtc\" IS NULL");

        builder.Property(x => x.ProfileResetAtUtc);

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

        builder.Property(x => x.PreferredPaymentMethodType)
            .HasMaxLength(30)
            .IsRequired(false);

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

