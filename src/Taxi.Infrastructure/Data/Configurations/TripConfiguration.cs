using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Admins;
using Taxi.Domain.Drivers;
using Taxi.Domain.Trips;
using Taxi.Domain.Users;
using Taxi.Domain.Vehicles;

namespace Taxi.Infrastructure.Data.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);

        // DriverId references the Drivers table (the Drivers PK), matching how
        // the whole codebase assigns/queries it. (It previously referenced
        // DomainUsers, which broke every driver assignment with an FK violation.)
        builder.HasOne<Driver>()
            .WithMany()
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AdminProfile>()
            .WithMany()
            .HasForeignKey(t => t.AcceptedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<VehicleType>()
            .WithMany()
            .HasForeignKey(t => t.VehicleTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PricingQuote>()
            .WithMany()
            .HasForeignKey(t => t.QuoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsConcurrencyToken();

        builder.OwnsMany(t => t.Stops, s =>
        {
            s.ToTable("TripStops");
            s.HasKey("Id");
            s.Property<Guid>("Id").ValueGeneratedOnAdd();
            s.OwnsOne(x => x.Coordinate, c =>
            {
                c.Property(p => p.Latitude).HasPrecision(18, 10);
                c.Property(p => p.Longitude).HasPrecision(18, 10);
            });
            s.Property(x => x.AddressLabel).HasMaxLength(500);
        });

        // MIGRATION TODO: next migration must drop the following nullable columns from Trips:
        // PickupAddress, PickupStreetName, PickupHouseNumber, PickupCoordinate_Latitude, PickupCoordinate_Longitude.
        builder.Property(t => t.ReferenceCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.PassengerNote)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(t => t.FlightNumber)
            .HasMaxLength(15)
            .IsRequired(false);

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired();

        builder.Property(t => t.LastModifiedUtc)
            .IsRequired(false);

        builder.Property(t => t.ScheduledAtUtc)
            .IsRequired(false);

        builder.Property(t => t.AssignedAtUtc)
            .IsRequired(false);

        builder.Property(t => t.AcceptedByAdminId)
            .IsRequired(false)
            .IsConcurrencyToken();

        builder.Property(t => t.AcceptedAtUtc)
            .IsRequired(false);

        builder.Property(t => t.ArrivedAtUtc)
            .IsRequired(false);

        builder.Property(t => t.StartedAtUtc)
            .IsRequired(false);

        builder.Property(t => t.CompletedAtUtc)
            .IsRequired(false);

        builder.Property(t => t.DeletedAtUtc)
            .IsRequired(false);

        builder.Property(t => t.UnacceptedReminder60SentAtUtc).IsRequired(false);
        builder.Property(t => t.UnacceptedReminder30SentAtUtc).IsRequired(false);
        builder.Property(t => t.UnacceptedReminder15SentAtUtc).IsRequired(false);
        builder.Property(t => t.UnacceptedOverdueSentAtUtc).IsRequired(false);
        builder.Property(t => t.AcceptedReminder30SentAtUtc).IsRequired(false);
        builder.Property(t => t.AcceptedReminder15SentAtUtc).IsRequired(false);

        builder.Property(t => t.NoDriverPromptDueAtUtc).IsRequired(false);
        builder.Property(t => t.NoDriverDecisionRequired)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.PassengerCount)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(t => t.BagCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.PassengerRating)
            .IsRequired(false);

        builder.Property(t => t.RatingComment)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.HasQueryFilter(t => t.DeletedAtUtc == null);
        builder.HasIndex(t => new { t.Status, t.ScheduledAtUtc });
        builder.HasIndex(t => new { t.Status, t.NoDriverPromptDueAtUtc });
        builder.HasIndex(t => t.AcceptedByAdminId);
    }
}

