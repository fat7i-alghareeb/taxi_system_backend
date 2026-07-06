using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Taxi.Domain.Auth;

namespace Taxi.Infrastructure.Data.Configurations;

public sealed class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Channel)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Purpose)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Recipient)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.CodeHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.MaxAttempts).IsRequired();
        builder.Property(x => x.FailedAttempts).IsRequired();
        builder.Property(x => x.ResendCount).IsRequired();
        builder.Property(x => x.LastSentAtUtc).IsRequired();

        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.DeviceId).HasMaxLength(256);
        builder.Property(x => x.ProviderMessageId).HasMaxLength(256);

        // Lookup path for cooldown checks and cleanup.
        builder.HasIndex(x => new { x.Recipient, x.Channel, x.Purpose, x.CreatedAtUtc });
    }
}
