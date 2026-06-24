using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Taxi.Domain.Trips;

namespace Taxi.Infrastructure.Data.Configurations;

public class TripRecordingConfiguration : IEntityTypeConfiguration<TripRecording>
{
    public void Configure(EntityTypeBuilder<TripRecording> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.FileUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.DeletedAtUtc)
            .IsRequired(false);

        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        // Recordings are always listed per-trip in capture order.
        builder.HasIndex(x => new { x.TripId, x.RecordedAtUtc });

        builder.HasQueryFilter(x => x.DeletedAtUtc == null);
    }
}
