using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Taxi.Domain.Identity;

namespace Taxi.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Token)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(r => r.UserId)
               .IsRequired();

        builder.Property(r => r.ExpiresOnUtc)
               .IsRequired();

        builder.Property(r => r.RevokedAtUtc);

        builder.Property(r => r.ReplacedByTokenId);

        builder.HasIndex(r => r.Token).IsUnique();

        builder.HasIndex(r => r.UserId);
    }
}

