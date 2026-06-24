using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Admins;

namespace Taxi.Infrastructure.Data.Configurations;

public class AdminProfileConfiguration : IEntityTypeConfiguration<AdminProfile>
{
    public void Configure(EntityTypeBuilder<AdminProfile> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Phone1)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(x => x.Phone2)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.Property(x => x.FcmToken)
            .HasMaxLength(4096)
            .IsRequired(false);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.LastModifiedUtc)
            .IsRequired(false);

        builder.HasIndex(x => x.Email)
            .IsUnique();
    }
}
