using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Invoices;
using Taxi.Domain.Trips;
using Taxi.Domain.Users;

namespace Taxi.Infrastructure.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TripId).IsUnique();
        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
        builder.HasIndex(x => x.PassengerId);

        builder.Property(x => x.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.NetAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxRate).HasPrecision(5, 4);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.GrossAmount).HasPrecision(18, 2);
        builder.Property(x => x.DistanceKm).HasPrecision(10, 3);
        builder.Property(x => x.DurationMin).HasPrecision(10, 2);

        builder.Property(x => x.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.PaymentReference).HasMaxLength(255);
        builder.Property(x => x.IssuerName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.IssuerAddress).IsRequired().HasMaxLength(500);
        builder.Property(x => x.IssuerVatNumber).HasMaxLength(64);
        builder.Property(x => x.TripReferenceCode).IsRequired().HasMaxLength(20);
        builder.Property(x => x.VehicleTypeName).IsRequired().HasMaxLength(120);
        builder.Property(x => x.PassengerName).HasMaxLength(200);
        builder.Property(x => x.PassengerPhone).HasMaxLength(30);
        builder.Property(x => x.PassengerEmail).HasMaxLength(150);
        builder.Property(x => x.PassengerAddress).HasMaxLength(500);
        builder.Property(x => x.StripePaymentMethodType).HasMaxLength(50);
        builder.Property(x => x.StopsJson).IsRequired().HasColumnType("jsonb");

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.LastModifiedUtc).IsRequired(false);
    }
}
