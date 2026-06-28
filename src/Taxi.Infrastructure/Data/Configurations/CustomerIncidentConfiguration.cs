using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.CustomerIncidents;
using Taxi.Domain.Trips;
using Taxi.Domain.Users;

namespace Taxi.Infrastructure.Data.Configurations;

public sealed class CustomerIncidentConfiguration : IEntityTypeConfiguration<CustomerIncident>
{
    public void Configure(EntityTypeBuilder<CustomerIncident> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2000);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3);
        builder.Property(x => x.Notes).HasColumnType("text");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Restrict);

        // Keeps auto-logging idempotent: the same source domain event can be
        // dispatched more than once by the outbox, but only one row is ever written.
        builder.HasIndex(x => x.SourceEventId).IsUnique();
        builder.HasIndex(x => x.PassengerId);
        builder.HasIndex(x => x.Type);
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
    }
}
