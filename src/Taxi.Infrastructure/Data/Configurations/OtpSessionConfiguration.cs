using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Auth;

namespace Taxi.Infrastructure.Data.Configurations;

public class OtpSessionConfiguration : IEntityTypeConfiguration<OtpSession>
{
    public void Configure(EntityTypeBuilder<OtpSession> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Phone)
            .IsRequired()
            .HasMaxLength(20);
    }
}
