using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Taxi.Domain.Trips;

namespace Taxi.Infrastructure.Data.Configurations;

public class TripMessageConfiguration : IEntityTypeConfiguration<TripMessage>
{
    public void Configure(EntityTypeBuilder<TripMessage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SenderRole)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Content)
            .HasMaxLength(TripMessage.MaxContentLength);

        builder.Property(x => x.PhotoUrl)
            .HasMaxLength(2048);

        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        // Chat history is always loaded per-trip in send order.
        builder.HasIndex(x => new { x.TripId, x.SentAtUtc });
    }
}
