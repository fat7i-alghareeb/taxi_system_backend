using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Trips;

namespace Taxi.Infrastructure.Data.Configurations;

public sealed class TripWaitingSessionConfiguration : IEntityTypeConfiguration<TripWaitingSession>
{
    public void Configure(EntityTypeBuilder<TripWaitingSession> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EstimatedFee).HasPrecision(18, 2);
        builder.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TripId);
        builder.HasIndex(x => new { x.TripId, x.StoppedAtUtc });
    }
}
